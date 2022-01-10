using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

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
                    float cost = 1f;
                    OriginExtraDrops oed = victimMaster.GetComponent<OriginExtraDrops>();
                    if (oed) cost = oed.cost;
                    Origin.DropItem(damageReport.victimBody.corePosition, treasureRng, cost);
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

        private int CompareEliteTierCost(CombatDirector.EliteTierDef tier1, CombatDirector.EliteTierDef tier2)
        {
            if (tier1.costMultiplier == tier2.costMultiplier)
            {
                return 0;
            }
            else
            {
                if (tier1.costMultiplier > tier2.costMultiplier)
                {
                    return -1;
                }
                return 1;
            }
        }

        IEnumerator PerformInvasion(Xoroshiro128Plus rng)
        {
            bool honorEnabled = CombatDirector.IsEliteOnlyArtifactActive();

            //Select spawncard
            SpawnCard spawnCard = Origin.SelectSpawnCard(rng);
            if (spawnCard)
            {
                int teamBeadCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.LunarTrinket.itemIndex, true, true);

                float playerFactor = 1f + (0.3f * (run.livingPlayerCount - 1));
                float invasionCount = 1f;
                int cycle = this.GetCurrentInvasionCycle();
                if (cycle > 0)
                {
                    invasionCount += (this.GetCurrentInvasionCycle() - 1) * Origin.extraBossesPerInvasion;
                }
                float loopFactor = 1 + 0.5f * (run.stageClearCount / 5);
                float spawnCredits = (playerFactor + teamBeadCount * beadBossCount) * invasionCount * loopFactor;

                while (spawnCredits > 0f && run.livingPlayerCount > 0)
                {
                    for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        if (!(spawnCredits > 0f && run.livingPlayerCount > 0)) break;

                        CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[i];
                        CharacterBody cb = characterMaster.bodyInstanceObject.GetComponent<CharacterBody>();
                        if (characterMaster.teamIndex == TeamIndex.Player
                            && characterMaster.playerCharacterMasterController
                            && cb && cb.healthComponent && cb.healthComponent.alive)
                        {
                            CombatSquad combatSquad = null;
                            Transform spawnOnTarget;
                            DirectorCore.MonsterSpawnDistance input;

                            spawnOnTarget = characterMaster.GetBody().coreTransform;
                            input = DirectorCore.MonsterSpawnDistance.Close;

                            //Select Elite Tier
                            CombatDirector.EliteTierDef selectedEliteTier = null;
                            EliteDef selectedElite = null;
                            float cost = 1f;
                            if (Origin.combineSpawns)
                            {

                                List<CombatDirector.EliteTierDef> eliteTiersList = EliteAPI.GetCombatDirectorEliteTiers().ToList();
                                eliteTiersList.Sort(CompareEliteTierCost);
                                foreach (CombatDirector.EliteTierDef etd in eliteTiersList)
                                {
                                    if (etd.costMultiplier <= spawnCredits && etd.eliteTypes.Length > 0 && etd.isAvailable(spawnCard.eliteRules))
                                    {
                                        selectedEliteTier = etd;
                                        selectedElite = GetRandomElite(etd);
                                        cost = etd.costMultiplier;
                                        break;
                                    }
                                }
                            }

                            if (honorEnabled && !Origin.ignoreHonor && selectedEliteTier == null && selectedElite == null)
                            {
                                selectedEliteTier = EliteAPI.VanillaEliteOnlyFirstTierDef;
                                selectedElite = GetRandomElite(selectedEliteTier);
                                cost = selectedEliteTier.costMultiplier;
                            }

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

                                    OriginExtraDrops oed = resultMaster.GetComponent<OriginExtraDrops>();
                                    if (!oed)
                                    {
                                        oed = resultMaster.gameObject.AddComponent<OriginExtraDrops>();
                                        oed.cost = cost;
                                    }

                                    if (selectedEliteTier != null && selectedElite != null)
                                    {
                                        resultMaster.inventory.GiveEquipmentString(selectedElite.eliteEquipmentDef.name);
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, (int)((selectedEliteTier.healthBoostCoefficient - 1f) * 10f));
                                        resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, (int)((selectedEliteTier.damageBoostCoefficient - 1f) * 10f));
                                    }
                                }
                            }));
                            GameObject spawnedObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                            if (spawnedObject)
                            {
                                spawnCredits -= cost;
                            }
                            if (combatSquad)
                            {
                                NetworkServer.Spawn(combatSquad.gameObject);
                                yield return new WaitForSeconds(spawnDelay);
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        public static EliteDef GetRandomElite(CombatDirector.EliteTierDef et)
        {
            return et.eliteTypes[UnityEngine.Random.Range(0, et.eliteTypes.Length)];
        }
    }

    public class OriginExtraDrops : MonoBehaviour
    {
        public float cost = 1f;
    }
}
