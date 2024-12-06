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
        public static float invasionInterval = 600f;

        public static float spawnDelay = 1f;
        public static int maxSpawns = 120;  //-1 disables limit
        public static float beadBossCount = 2.5f;

        private List<DirectorSpawnRequest> pendingSpawns;
        
        public SpawnCard prevBoss = null;

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
            return Mathf.FloorToInt(this.run.GetRunStopwatch() / invasionInterval);
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
                    if (!(damageReport.victimBody && damageReport.victimBody.healthComponent && damageReport.victimBody.healthComponent.alive))
                    {
                        float cost = 1f;
                        OriginExtraDrops oed = victimMaster.GetComponent<OriginExtraDrops>();
                        if (oed) cost = oed.cost;
                        Origin.DropItem(damageReport.victimBody.corePosition, treasureRng, cost, (damageReport.victim && oed.teleportItem) ? damageReport.victim.gameObject : null, oed.bossDrop);
                    }
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

            CombatSquad combatSquad = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/ShadowCloneEncounter")).GetComponent<CombatSquad>();

            //Select spawncard
            SpawnCard spawnCard = Origin.SelectSpawnCard(rng, ref prevBoss);
            if (spawnCard)
            {
                int teamBeadCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.LunarTrinket.itemIndex, true, true);

                float playerFactor = 1f + (0.3f * (run.livingPlayerCount - 1));
                float invasionCount = 1f;
                int cycle = this.GetCurrentInvasionCycle();
                SceneDef sd = RoR2.SceneCatalog.GetSceneDefForCurrentScene();
                if (cycle > 0 && sd && sd.sceneType == SceneType.Stage)
                {
                    invasionCount += (this.GetCurrentInvasionCycle() - 1) * Origin.extraBossesPerInvasion;
                }
                //float loopFactor = 1 + 0.5f * (run.stageClearCount / 5);
                float spawnCredits = (playerFactor + teamBeadCount * beadBossCount) * invasionCount;// * loopFactor;
                if (spawnCard == Origin.scavCard) spawnCredits *= 0.5f;

                while (spawnCredits > 0f && run.livingPlayerCount > 0)
                {
                    for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        if (!(spawnCredits > 0f && run.livingPlayerCount > 0)) break;

                        CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[i];
                        GameObject bodyInstanceObject = characterMaster.bodyInstanceObject;

                        if (!bodyInstanceObject) continue;
                        CharacterBody cb = bodyInstanceObject.GetComponent<CharacterBody>();
                        if (cb && characterMaster.teamIndex == TeamIndex.Player
                            && characterMaster.playerCharacterMasterController
                            && cb && cb.healthComponent && cb.healthComponent.alive)
                        {
                            //CombatSquad combatSquad = null;
                            Transform spawnOnTarget;
                            DirectorCore.MonsterSpawnDistance input;

                            spawnOnTarget = characterMaster.GetBody().coreTransform;
                            input = DirectorCore.MonsterSpawnDistance.Standard;

                            //Select Elite Tier
                            CombatDirector.EliteTierDef selectedEliteTier = null;
                            EliteDef selectedElite = null;
                            float cost = 1f;
                            if (Origin.combineSpawns)
                            {
                                List<CombatDirector.EliteTierDef> eliteTiersList = EliteAPI.GetCombatDirectorEliteTiers().ToList();
                                eliteTiersList.Sort(Utils.CompareEliteTierCost);
                                //Ordered with the most expensive at the top
                                foreach (CombatDirector.EliteTierDef etd in eliteTiersList)
                                {
                                    //Debug.Log(etd.costMultiplier);
                                    if (etd.costMultiplier <= spawnCredits && etd.eliteTypes.Length > 0 && etd.isAvailable(spawnCard.eliteRules))
                                    {
                                        selectedEliteTier = etd;
                                        selectedElite = Utils.GetRandomElite(etd);
                                        cost = etd.costMultiplier;
                                        break;
                                    }
                                }
                            }

                            if (honorEnabled && !Origin.ignoreHonor && selectedEliteTier == null && selectedElite == null)
                            {
                                selectedEliteTier = EliteAPI.VanillaEliteOnlyFirstTierDef;
                                selectedElite = selectedEliteTier.GetRandomAvailableEliteDef(rng);
                                cost = selectedEliteTier.costMultiplier;
                            }

                            DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                            {
                                spawnOnTarget = spawnOnTarget,
                                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                                //minDistance = 5f, maxDistance = Mathf.Infinity    //doesnt seem to actually do anything
                            };
                            DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, rng);
                            directorSpawnRequest.teamIndexOverride = new TeamIndex?(Origin.bossVoidTeam ? TeamIndex.Void : TeamIndex.Monster);
                            directorSpawnRequest.ignoreTeamMemberLimit = true;
                            directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                            {
                                /*if (!combatSquad)
                                {
                                    combatSquad = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/ShadowCloneEncounter")).GetComponent<CombatSquad>();
                                }*/
                                if (result.spawnedInstance)
                                {
                                    CharacterMaster resultMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                                    if (resultMaster)
                                    {
                                        combatSquad.AddMember(resultMaster);
                                        if (resultMaster.inventory)
                                        {
                                            resultMaster.inventory.GiveItem(Origin.OriginBonusItem);
                                            resultMaster.inventory.RemoveItem(RoR2Content.Items.InvadingDoppelganger);
                                            if (Origin.useAdaptiveArmor && run.stageClearCount >= 5)
                                            {
                                                resultMaster.inventory.GiveItem(RoR2Content.Items.AdaptiveArmor);
                                            }
                                            if (resultMaster.inventory.GetItemCount(RoR2Content.Items.UseAmbientLevel) <= 0)
                                            {
                                                resultMaster.inventory.GiveItem(RoR2Content.Items.UseAmbientLevel);
                                            }

                                            if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.MonsterTeamGainsItems) && directorSpawnRequest.teamIndexOverride != TeamIndex.Monster)
                                            {
                                                resultMaster.inventory.AddItemsFrom(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.monsterTeamInventory);
                                            }
                                        }

                                        OriginExtraDrops oed = resultMaster.GetComponent<OriginExtraDrops>();
                                        if (!oed)
                                        {
                                            oed = resultMaster.gameObject.AddComponent<OriginExtraDrops>();
                                            if (resultMaster.bodyPrefab)
                                            {
                                                DeathRewards dr = resultMaster.bodyPrefab.GetComponent<DeathRewards>();
                                                if (dr && dr.bossPickup.pickupName != null)
                                                {
                                                    oed.bossDrop = (PickupIndex)dr.bossPickup;
                                                }
                                                else
                                                {
                                                    oed.bossDrop = PickupIndex.none;
                                                }
                                            }
                                            oed.cost = cost;
                                            if (spawnCard == Origin.wormCard)
                                            {
                                                oed.teleportItem = true;
                                            }
                                        }

                                        if (selectedEliteTier != null && selectedElite != null)
                                        {
                                            resultMaster.inventory.GiveEquipmentString(selectedElite.eliteEquipmentDef.name);
                                            resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, (int)((selectedElite.healthBoostCoefficient - 1f) * 10f));
                                            resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, (int)((selectedElite.damageBoostCoefficient - 1f) * 10f));
                                        }
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
    }

    public class OriginExtraDrops : MonoBehaviour
    {
        public PickupIndex bossDrop = PickupIndex.none;
        public float cost = 1f;
        public bool teleportItem = false;
    }
}
