using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.AddressableAssets;

namespace Risky_Artifacts.Artifacts.MonoBehaviours
{
    public class BrotherInvasionController : MonoBehaviour
    {
        public static float minInvasionTimer = 330f;
        public static float maxInvasionTimer = 420f;
        public static CharacterSpawnCard spawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Brother/cscBrother.asset").WaitForCompletion();

        private bool triggeredInvasion;
        private float stopwatch;
        private float invasionTime;

        private bool artifactIsEnabled
        {
            get
            {
                return RunArtifactManager.instance.IsArtifactEnabled(BrotherInvasion.artifact);
            }
        }

        public void Awake()
        {
            triggeredInvasion = false;
            stopwatch = 0f;
            invasionTime = UnityEngine.Random.Range(BrotherInvasionController.minInvasionTimer, BrotherInvasionController.maxInvasionTimer);
        }

        public void FixedUpdate()
        {
            if (!triggeredInvasion)
            {
                stopwatch += Time.fixedDeltaTime;

                if (stopwatch >= invasionTime)
                {
                    triggeredInvasion = true;
                    if (artifactIsEnabled)
                    {
                        Util.PlaySound("Play_UI_arenaMode_wave_startBoss", base.gameObject);
                        if (NetworkServer.active) RunInvasion(new Xoroshiro128Plus(Run.instance.seed + (ulong)Run.instance.stageClearCount));
                    }
                }
            }
            else
            {
                Destroy(this);
            }
        }

        public void RunInvasion(Xoroshiro128Plus rng)
        {
            Transform spawnOnTarget = null;
            if (TeleporterInteraction.instance)
            {
                spawnOnTarget = TeleporterInteraction.instance.transform;
            }
            else
            {
                //There's probably a better way to do this
                List<CharacterBody> playerList = CharacterBody.readOnlyInstancesList.Where(cb => (cb.isPlayerControlled && cb.teamComponent && cb.teamComponent.teamIndex == TeamIndex.Player)).ToList();
                if (playerList.Count > 0)
                {
                    CharacterBody firstBody = playerList.FirstOrDefault();
                    if (firstBody)
                    {
                        spawnOnTarget = firstBody.transform;
                    }
                }
            }

            if (spawnOnTarget && Run.instance)
            {
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode
                };

                DirectorCore.GetMonsterSpawnDistance(DirectorCore.MonsterSpawnDistance.Far, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);

                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, rng);
                directorSpawnRequest.teamIndexOverride = new TeamIndex?(BrotherInvasion.bossLunarTeam ? TeamIndex.Lunar : TeamIndex.Monster);
                directorSpawnRequest.ignoreTeamMemberLimit = true;

                CombatSquad combatSquad = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/ShadowCloneEncounter")).GetComponent<CombatSquad>();
                directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult result)
                {
                    CharacterMaster resultMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                    if (resultMaster)
                    {
                        combatSquad.AddMember(resultMaster);

                        if (resultMaster.inventory)
                        {
                            resultMaster.inventory.RemoveItem(RoR2Content.Items.InvadingDoppelganger);
                            resultMaster.inventory.GiveItem(RoR2Content.Items.TeleportWhenOob);
                            resultMaster.inventory.GiveItem(BrotherInvasion.BrotherInvasionBonusItem);
                            resultMaster.inventory.GiveItem(RoR2Content.Items.AdaptiveArmor);
                            if (resultMaster.inventory.GetItemCount(RoR2Content.Items.UseAmbientLevel) <= 0)
                            {
                                resultMaster.inventory.GiveItem(RoR2Content.Items.UseAmbientLevel);
                            }

                            float hpMult = 12f; //12f for OL Worm HP
                            float damageMult = 2f;

                            EliteDef selectedElite = null;
                            bool honorEnabled = CombatDirector.IsEliteOnlyArtifactActive();
                            if (honorEnabled && !BrotherInvasion.ignoreHonor)
                            {
                                selectedElite = EliteAPI.VanillaEliteOnlyFirstTierDef.GetRandomAvailableEliteDef(rng);
                            }
                            if (selectedElite && selectedElite.eliteEquipmentDef)
                            {
                                resultMaster.inventory.SetEquipmentIndex(selectedElite.eliteEquipmentDef.equipmentIndex);
                                hpMult *= selectedElite.healthBoostCoefficient;
                                damageMult *= selectedElite.damageBoostCoefficient;
                            }

                            int hpBoostToGive = Mathf.FloorToInt((hpMult - 1f) * 10f);
                            int damageBoostToGive = Mathf.FloorToInt((damageMult - 1f) * 10f);
                            if (hpBoostToGive > 0) resultMaster.inventory.GiveItem(RoR2Content.Items.BoostHp, hpBoostToGive);
                            if (damageBoostToGive > 0) resultMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, damageBoostToGive);
                        }

                        CharacterBody cb = resultMaster.GetBody();
                        if (cb)
                        {
                            cb.gameObject.AddComponent<BrotherInvasionHauntOnDeath>();
                        }
                    }
                }));
                GameObject spawnedObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                NetworkServer.Spawn(combatSquad.gameObject);
            }
            else
            {
                Debug.LogError("Risky Artifacts: No spawn position found for BrotherInvasion.");
            }

            Destroy(this);
        }
    }
}
