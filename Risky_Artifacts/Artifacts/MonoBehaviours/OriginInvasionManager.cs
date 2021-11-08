using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections;

namespace Risky_Artifacts.Artifacts.MonoBehaviours
{
    //Mostly based off of DoppelgangerInvasionManager
    public class OriginInvasionManager : MonoBehaviour
    {
        private int previousInvasionCycle;
        private ulong seed;
        private Run run;
        private Xoroshiro128Plus treasureRng;
        private float invasionInterval = 600f;

        public static float spawnDelay = 1.2f;
        public static int maxSpawns = 100;  //-1 disables limit
        public static int beadBossCount = 1;

        private List<DirectorSpawnRequest> pendingSpawns;

        private bool artifactIsEnabled
        {
            get
            {
                return RunArtifactManager.instance.IsArtifactEnabled(Origin.artifact);
            }
        }
        private void FixedUpdate()
        {
            int currentInvasionCycle = this.GetCurrentInvasionCycle();
            if (this.previousInvasionCycle < currentInvasionCycle)
            {
                this.previousInvasionCycle = currentInvasionCycle;
                if (this.artifactIsEnabled)
                {
                    StartCoroutine(PerformInvasion(new Xoroshiro128Plus(this.seed + (ulong)((long)currentInvasionCycle))));
                }
            }
        }
        private int GetCurrentInvasionCycle()
        {
            return Mathf.FloorToInt(this.run.GetRunStopwatch() / this.invasionInterval);
        }

        private static void OnRunStartGlobal(Run run)
        {
            if (NetworkServer.active)
            {
                run.gameObject.AddComponent<OriginInvasionManager>();
            }
        }

        public static void Init()
        {
            Run.onRunStartGlobal += OnRunStartGlobal;
        }
        private void OnEnable()
        {
            GlobalEventManager.onCharacterDeathGlobal += this.OnCharacterDeathGlobal;
            ArtifactTrialMissionController.onShellTakeDamageServer += this.OnArtifactTrialShellTakeDamageServer;
        }
        private void OnDisable()
        {
            GlobalEventManager.onCharacterDeathGlobal -= this.OnCharacterDeathGlobal;
            ArtifactTrialMissionController.onShellTakeDamageServer -= this.OnArtifactTrialShellTakeDamageServer;
        }

        private void Start()
        {
            this.run = base.GetComponent<Run>();
            this.seed = this.run.seed;
            this.treasureRng = new Xoroshiro128Plus(this.seed);
            pendingSpawns = new List<DirectorSpawnRequest>();
        }

        private void OnCharacterDeathGlobal(DamageReport damageReport)
        {
            CharacterMaster victimMaster = damageReport.victimMaster;
            Inventory inventory = (victimMaster != null) ? victimMaster.inventory : null;
            if (inventory)
            {
                if (inventory.GetItemCount(Origin.OriginBonusItem) > 0 && inventory.GetItemCount(RoR2Content.Items.ExtraLife) == 0 && !damageReport.victimMaster.minionOwnership.ownerMaster)
                {
                    Origin.DropItem(damageReport.victimBody.corePosition, treasureRng);
                }
            }
        }

        private void OnArtifactTrialShellTakeDamageServer(ArtifactTrialMissionController missionController, DamageReport damageReport)
        {
            if (!this.artifactIsEnabled)
            {
                return;
            }
            if (!damageReport.victim.alive)
            {
                return;
            }
            StartCoroutine(PerformInvasion(new Xoroshiro128Plus((ulong)damageReport.victim.health)));
        }

        IEnumerator PerformInvasion(Xoroshiro128Plus rng)
        {
            //Select spawncard
            SpawnCard spawnCard = Origin.SelectSpawnCard(rng);
            if (spawnCard)
            {
                EliteDef selectedT1Elite = null;
                float t1EliteHpMult = 1f;
                float t1EliteDamageMult = 1f;

                if (CombatDirector.IsEliteOnlyArtifactActive())
                {
                    CombatDirector.EliteTierDef t1Elite = CombatDirector.eliteTiers[1];
                    t1EliteHpMult = t1Elite.healthBoostCoefficient;
                    t1EliteDamageMult = t1Elite.damageBoostCoefficient;
                    selectedT1Elite = t1Elite.eliteTypes[rng.RangeInt(0, t1Elite.eliteTypes.Length)];
                }

                int livingPlayers = run.livingPlayerCount;
                int spawnCount = Mathf.FloorToInt(0.5f + 0.5f * livingPlayers) * (1 + run.stageClearCount/5);
                int spawnsPerPlayer = Math.Max(Mathf.CeilToInt((float)spawnCount / (float)livingPlayers), 1);
                int spawned = 0;

                for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
                {
                    if (spawnCount <= 0)
                    {
                        break;
                    }
                    CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[i];
                    CharacterBody cb = characterMaster.bodyInstanceObject.GetComponent<CharacterBody>();
                    if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.playerCharacterMasterController && cb && cb.healthComponent && cb.healthComponent.alive)
                    {
                        int toSpawn = 0;
                        if (spawnCount > spawnsPerPlayer)
                        {
                            toSpawn = spawnsPerPlayer;
                            spawnCount -= spawnsPerPlayer;
                        }
                        else
                        {
                            toSpawn = spawnCount;
                            spawnCount = 0;
                        }

                        if (characterMaster.inventory)
                        {
                            toSpawn += characterMaster.inventory.GetItemCount(RoR2Content.Items.LunarTrinket) * beadBossCount;
                        }
                        //spawnCount *= 1 + (Run.instance.stageClearCount / 5);
                        for (int j = 0; j < toSpawn; j++)
                        {
                            if (!spawnCard || (maxSpawns >= 0 && spawned >= maxSpawns))
                            {
                                yield return null;
                            }
                            Transform spawnOnTarget;
                            DirectorCore.MonsterSpawnDistance input;

                            spawnOnTarget = characterMaster.GetBody().coreTransform;
                            input = DirectorCore.MonsterSpawnDistance.Close;

                            DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                            {
                                spawnOnTarget = spawnOnTarget,
                                placementMode = DirectorPlacementRule.PlacementMode.NearestNode
                            };
                            DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, rng);
                            directorSpawnRequest.teamIndexOverride = new TeamIndex?(TeamIndex.Monster);
                            directorSpawnRequest.ignoreTeamMemberLimit = true;
                            CombatSquad combatSquad = null;
                            directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                            {
                                if (!combatSquad)
                                {
                                    combatSquad = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/ShadowCloneEncounter")).GetComponent<CombatSquad>();
                                }
                                combatSquad.AddMember(result.spawnedInstance.GetComponent<CharacterMaster>());
                                CharacterMaster resultMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                                if (resultMaster && resultMaster.inventory)
                                {
                                    resultMaster.inventory.GiveItem(Origin.OriginBonusItem);
                                    resultMaster.inventory.RemoveItem(RoR2Content.Items.InvadingDoppelganger);
                                    if (run.stageClearCount >= 5)
                                    {
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.AdaptiveArmor);
                                    }

                                    if (selectedT1Elite != null)
                                    {
                                        resultMaster.inventory.GiveEquipmentString(selectedT1Elite.eliteEquipmentDef.name);
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, (int)((t1EliteHpMult - 1f) * 10f));
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, (int)((t1EliteDamageMult - 1f) * 10f));
                                    }
                                }
                            }));
                            DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                            if (combatSquad)
                            {
                                NetworkServer.Spawn(combatSquad.gameObject);
                                spawned++;
                                yield return new WaitForSeconds(1.5f);
                            }
                        }
                    }
                }
                yield return null;
            }
        }
    }
}
