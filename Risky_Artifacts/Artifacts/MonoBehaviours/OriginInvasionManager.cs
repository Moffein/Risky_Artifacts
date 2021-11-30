using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections;
using static Risky_Artifacts.Artifacts.MonoBehaviours.OriginExtraDrops;

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

        public static float spawnDelay = 1f;
        public static int maxSpawns = 120;  //-1 disables limit
        public static float beadBossCount = 2.5f;

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
                    EliteTier tier = EliteTier.None;
                    OriginExtraDrops oed = victimMaster.GetComponent<OriginExtraDrops>();
                    if (oed) tier = oed.tier;
                    Origin.DropItem(damageReport.victimBody.corePosition, treasureRng, tier);
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
            bool honorEnabled = CombatDirector.IsEliteOnlyArtifactActive();

            //Select spawncard
            SpawnCard spawnCard = Origin.SelectSpawnCard(rng);
            if (spawnCard)
            {
                EliteDef selectedT1Elite = null;
                EliteDef selectedT2Elite = null;

                CombatDirector.EliteTierDef t1Elite = CombatDirector.eliteTiers[honorEnabled ? 2 : 1];
                selectedT1Elite = t1Elite.eliteTypes[rng.RangeInt(0, t1Elite.eliteTypes.Length)];

                CombatDirector.EliteTierDef t2Elite = CombatDirector.eliteTiers[3];
                selectedT2Elite = t2Elite.eliteTypes[rng.RangeInt(0, t2Elite.eliteTypes.Length)];

                int teamBeadCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.LunarTrinket.itemIndex, true, true);

                float spawnCredits = (1f + (0.3f * (run.livingPlayerCount - 1)) + teamBeadCount * beadBossCount) * (1 + 0.5f * (run.stageClearCount / 5));

                int spawnCount = 0;
                int t1Count = 0;
                int t2Count = 0;
                if (Origin.combineSpawns)
                {
                    while (spawnCredits > t2Elite.costMultiplier)
                    {
                        t2Count++;
                        spawnCredits -= t2Elite.costMultiplier;
                    }
                    if (honorEnabled && !Origin.ignoreHonor)
                    {
                        t1Count = Mathf.FloorToInt(spawnCredits);
                        spawnCredits = 0f;
                    }
                    else
                    {
                        while (spawnCredits > t1Elite.costMultiplier)
                        {
                            t1Count++;
                            spawnCredits -= t1Elite.costMultiplier;
                        }
                        spawnCount = Mathf.FloorToInt(spawnCredits);
                    }
                }
                else
                {
                    if (honorEnabled && !Origin.ignoreHonor)
                    {
                        t1Count = Mathf.FloorToInt(spawnCredits);
                        spawnCredits = 0f;
                    }
                    spawnCount = Mathf.FloorToInt(spawnCredits);
                }

                while ((spawnCount + t1Count + t2Count) > 0 && run.livingPlayerCount > 0)
                {
                    for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        if (!((spawnCount + t1Count + t2Count) > 0 && run.livingPlayerCount > 0)) yield return null;

                        CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[i];
                        CharacterBody cb = characterMaster.bodyInstanceObject.GetComponent<CharacterBody>();
                        if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.playerCharacterMasterController && cb && cb.healthComponent && cb.healthComponent.alive)
                        {
                            CombatSquad combatSquad = null;
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

                                    EliteTier et = EliteTier.None;
                                    if (t2Count > 0)
                                    {
                                        t2Count--;
                                        et = EliteTier.T2;
                                        resultMaster.inventory.GiveEquipmentString(selectedT2Elite.eliteEquipmentDef.name);
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, (int)((t2Elite.healthBoostCoefficient - 1f) * 10f));
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, (int)((t2Elite.damageBoostCoefficient - 1f) * 10f));
                                    }
                                    else if (t1Count > 0)
                                    {
                                        t1Count--;
                                        et = EliteTier.T1;
                                        resultMaster.inventory.GiveEquipmentString(selectedT1Elite.eliteEquipmentDef.name);
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, (int)((t1Elite.healthBoostCoefficient - 1f) * 10f));
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, (int)((t1Elite.damageBoostCoefficient - 1f) * 10f));
                                    }
                                    else
                                    {
                                        spawnCount--;
                                    }

                                    if (et != EliteTier.None)
                                    {
                                        OriginExtraDrops oed = resultMaster.GetComponent<OriginExtraDrops>();
                                        if (!oed)
                                        {
                                            oed = resultMaster.gameObject.AddComponent<OriginExtraDrops>();
                                            oed.tier = et;
                                        }
                                    }
                                }
                            }));
                            DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                            if (combatSquad)
                            {
                                NetworkServer.Spawn(combatSquad.gameObject);
                                spawnCount--;
                                yield return new WaitForSeconds(spawnDelay);
                            }
                        }
                    }
                }
                yield return null;
            }
        }
    }

    public class OriginExtraDrops : MonoBehaviour
    {
        public EliteTier tier = EliteTier.None;
        public enum EliteTier
        {
            None, T1, T2
        }
    }
}
