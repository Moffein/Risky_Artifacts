using RoR2;
using R2API;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using System.Security.Cryptography;
using UnityEngine.Networking;
using System.Linq;

namespace Risky_Artifacts.Artifacts
{
    //This is so scuffed.
    public class Cruelty
    {
        public static List<EquipmentDef> BlacklistedElites = new List<EquipmentDef>();
        public static ArtifactDef artifact;
        public static bool enabled = true;
        public static bool guaranteeSpecialBoss = true;
        public static int runEndBossGeneralAffixes = 3;
        public static int runEndBossT2Affixes = 0;
        public static ScalingMode damageScaling;
        public static ScalingMode healthScaling;
        public static ScalingMode costScaling;
        public static ScalingMode rewardScaling;

        public static int maxT2Affixes = 1;
        public static int maxGeneralAffixes = 3;

        public static float triggerChance = 25f;
        public static float failureChance = 25f;

        private static EquipmentDef MalachiteDef, CelestineDef;

        public enum ScalingMode
        {
            None, Additive, Multiplicative
        }

        public Cruelty()
        {
            if (!enabled) return;

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfCruelty";
            artifact.nameToken = "RISKYARTIFACTS_CRUELTY_NAME";
            artifact.descriptionToken = healthScaling == ScalingMode.Multiplicative || damageScaling == ScalingMode.Multiplicative ? "RISKYARTIFACTS_CRUELTY_DESC_MULT" : "RISKYARTIFACTS_CRUELTY_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texCrueltyDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texCrueltyEnabled.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            On.RoR2.CombatDirector.Awake += CombatDirector_Awake;

            //Special bosses arent affected by the CombatDirector hook
            On.RoR2.ScriptedCombatEncounter.BeginEncounter += ScriptedCombatEncounter_BeginEncounter;

            RoR2.RoR2Application.onLoad += OnLoad;
        }

        private void ScriptedCombatEncounter_BeginEncounter(On.RoR2.ScriptedCombatEncounter.orig_BeginEncounter orig, ScriptedCombatEncounter self)
        {
            if (NetworkServer.active && self.combatSquad)
            {
                self.combatSquad.onMemberAddedServer += CombatSquadCruelty;
            }
            orig(self);
        }


        private void CombatSquadCruelty(CharacterMaster master)
        {
            if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Cruelty.artifact) && guaranteeSpecialBoss)
            {
                if (master && master.inventory && master.inventory.GetItemCount(RoR2Content.Items.HealthDecay) <= 0)
                {
                    CharacterBody body = master.GetBody();
                    if (body)
                        Cruelty.CreateCrueltyElite(body, master.inventory, Mathf.Infinity, 0, Cruelty.failureChance, true, -1, true);
                }
            }
        }

        private void CombatDirector_Awake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
        {
            orig(self);
            self.onSpawnedServer.AddListener(delegate (GameObject targetGameObject)
            {
                if (!NetworkServer.active || !(RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Cruelty.artifact))) return;

                DirectorCard lastAttemptedMonsterCard = self.lastAttemptedMonsterCard;
                float monsterCredit = self.monsterCredit;

                CharacterMaster master = targetGameObject.GetComponent<CharacterMaster>();
                if (master && master.inventory && master.inventory.GetItemCount(RoR2Content.Items.HealthDecay) <= 0)
                {
                    if (Hunted.HuntedStatItem && master.inventory.GetItemCount(Hunted.HuntedStatItem) > 0 && (!Hunted.allowCruelty || !Hunted.allowElite)) return;
                    CharacterBody body = master.GetBody();
                    if (!body || body.isPlayerControlled || body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Masterless)) return;

                    if ((body.isBoss || body.isChampion || Random.Range(1, 100) <= Cruelty.triggerChance))
                        self.monsterCredit -= Cruelty.CreateCrueltyElite(body, master.inventory, monsterCredit, lastAttemptedMonsterCard.cost, Cruelty.failureChance, false);
                }
            });
        }

        private void OnLoad()
        {
            EquipmentIndex blightIndex = EquipmentCatalog.FindEquipmentIndex("AffixBlightedMoffein");
            if (blightIndex != EquipmentIndex.None)
            {
                EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(blightIndex);
                if (ed && ed.passiveBuffDef && ed.passiveBuffDef.eliteDef)
                {
                    BlacklistedElites.Add(ed);
                }
            }

            EquipmentIndex perfectedIndex = EquipmentCatalog.FindEquipmentIndex("EliteLunarEquipment");
            if (perfectedIndex != EquipmentIndex.None)
            {
                EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(perfectedIndex);
                if (ed && ed.passiveBuffDef && ed.passiveBuffDef.eliteDef)
                {
                    BlacklistedElites.Add(ed);
                }
            }

            MalachiteDef = RoR2Content.Equipment.AffixPoison;
            CelestineDef = RoR2Content.Equipment.AffixHaunted;
        }

        public static float CreateCrueltyElite(CharacterBody characterBody, Inventory inventory, float currentDirectorCredits, int cardCost, float failureChance, bool ignoreAvailableCheck, int affixCount = -1, bool isSpecial = false)
        {
            if (!characterBody || !inventory) return 0f;
            float availableCredits = currentDirectorCredits;

            //Check amount of elite buffs the target has
            List<BuffIndex> currentEliteBuffs = new List<BuffIndex>();
            foreach (BuffIndex b in BuffCatalog.eliteBuffIndices)
            {
                if (characterBody.HasBuff(b) && !currentEliteBuffs.Contains(b))
                {
                    currentEliteBuffs.Add(b);
                }
            }
            //int eliteBuffs = currentEliteBuffs.Count();
            int addedAffixes = 0;
            int t2Count = 0;
            int generalCount = 0;

            bool hasEquip = inventory.GetEquipmentIndex() != EquipmentIndex.None;

            float currentHealthMult = 1f + inventory.GetItemCount(RoR2Content.Items.BoostHp) * 0.1f;
            float currentDamageMult = 1f + inventory.GetItemCount(RoR2Content.Items.BoostDamage) * 0.1f;

            float desiredHealthMult = currentHealthMult;
            float desiredDamageMult = currentDamageMult;

            bool isNotElite = currentEliteBuffs.Count <= 0 && currentHealthMult == 1f && currentDamageMult == 1f;

            List<EliteDef> selectedElites = new List<EliteDef>();

            //Roll for failure each time an affix is added.

            float totalCostMult = 1f;
            bool isT2 = false;

            float deathRewardsMultiplier = 1f;  //Only used for multiplicative DR scaling
            float deathRewardsAdd = 0f; //Used for additive DR scaling

            int maxT2 = isSpecial ? Cruelty.runEndBossT2Affixes : Cruelty.maxT2Affixes;
            int maxGeneral = isSpecial ? Cruelty.runEndBossGeneralAffixes : Cruelty.maxGeneralAffixes;

            //Iterate through all elites, starting from the most expensive
            //Seems very inefficient
            List<CombatDirector.EliteTierDef> eliteTiersList = EliteAPI.GetCombatDirectorEliteTiers().ToList();
            eliteTiersList.Sort(Utils.CompareEliteTierCost);
            foreach (CombatDirector.EliteTierDef etd in eliteTiersList)
            {
                //Super scuffed. Checking for the Elite Type directly didn't work.
                isT2 = false;
                if (etd.eliteTypes != null)
                {
                    foreach (EliteDef ed in etd.eliteTypes)
                    {
                        if (ed != null && (ed.eliteEquipmentDef == MalachiteDef || ed.eliteEquipmentDef == CelestineDef))
                        {
                            isT2 = true;
                            break;
                        }
                    }
                }

                List<EliteDef> availableElitesInTier = null;
                availableElitesInTier = (ignoreAvailableCheck && etd.eliteTypes != null ? etd.eliteTypes.ToList() : etd.availableDefs);

                if (availableElitesInTier == null) continue;
                availableElitesInTier = availableElitesInTier.Where(x => !selectedElites.Contains(x) && x != null).ToList();
                Utils.Shuffle(availableElitesInTier);

                foreach (EliteDef ed in availableElitesInTier)
                {
                    bool reachedTierLimit = (isT2 && maxT2 >= 0 && t2Count >= maxT2) || (!isT2 && maxGeneral >= 0 && generalCount >= maxGeneral);
                    if (reachedTierLimit) break;

                    //Check if EliteDef has an associated buff and the character doesn't already have the buff.
                    bool isBuffValid = ed && ed.eliteEquipmentDef
                        && ed.eliteEquipmentDef.passiveBuffDef
                        && ed.eliteEquipmentDef.passiveBuffDef.isElite
                        && !BlacklistedElites.Contains(ed.eliteEquipmentDef)
                        && !currentEliteBuffs.Contains(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
                    if (!isBuffValid) continue;

                    bool hasEnoughCredits = true;
                    switch (costScaling)
                    {
                        case ScalingMode.Multiplicative:
                            //Always calculate multiplicative off of the total credits to maintain consistency.
                            hasEnoughCredits = currentDirectorCredits - cardCost * totalCostMult * etd.costMultiplier >= 0f;
                            break;
                        case ScalingMode.Additive:
                            hasEnoughCredits = availableCredits - (cardCost * (etd.costMultiplier - 1f)) >= 0f;
                            break;
                        default:
                            hasEnoughCredits = true;
                            break;
                    }

                    if (hasEnoughCredits && ((affixCount > 0 && addedAffixes < affixCount) || (affixCount <= 0 && UnityEngine.Random.Range(1, 100) > Cruelty.failureChance)))
                    {
                        switch (costScaling)
                        {
                            case ScalingMode.Multiplicative:
                                //Always calculate multiplicative off of the total credits to maintain consistency.
                                availableCredits = currentDirectorCredits - cardCost * totalCostMult * etd.costMultiplier;
                                totalCostMult *= etd.costMultiplier;
                                break;
                            case ScalingMode.Additive:
                                availableCredits -= cardCost * (etd.costMultiplier - 1f);
                                break;
                            default:
                                break;
                        }

                        if (etd.costMultiplier > 1f)
                        {
                            switch (rewardScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    deathRewardsMultiplier *= etd.costMultiplier;
                                    break;
                                case ScalingMode.Additive:
                                    deathRewardsAdd += etd.costMultiplier - 1f;
                                    break;
                                default:
                                    break;
                            }
                        }

                        //Fill in equipment slot if it isn't filled
                        if (!hasEquip && ed.eliteEquipmentDef)
                        {
                            inventory.SetEquipmentIndex(ed.eliteEquipmentDef.equipmentIndex);
                            hasEquip = true;
                        }

                        //Apply Elite Bonus
                        currentEliteBuffs.Add(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
                        characterBody.AddBuff(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
                        addedAffixes++;
                        if (isT2)
                        {
                            t2Count++;
                        }
                        else
                        {
                            generalCount++;
                        }

                        if (isNotElite)
                        {
                            desiredDamageMult = ed.damageBoostCoefficient;
                            desiredHealthMult = ed.healthBoostCoefficient;
                            isNotElite = false;
                        }
                        else
                        {
                            switch (Cruelty.damageScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    desiredDamageMult *= ed.damageBoostCoefficient;
                                    break;
                                case ScalingMode.Additive:
                                    desiredDamageMult += ed.damageBoostCoefficient - 1f;
                                    break;
                                default:
                                    break;
                            }

                            switch (Cruelty.healthScaling)
                            {
                                case ScalingMode.Multiplicative:
                                    desiredHealthMult *= ed.healthBoostCoefficient;
                                    break;
                                case ScalingMode.Additive:
                                    desiredHealthMult += ed.healthBoostCoefficient - 1f;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            int boostDamagecount = Mathf.FloorToInt((desiredDamageMult - currentDamageMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostDamage, boostDamagecount);

            int boostHealthcount = Mathf.FloorToInt((desiredHealthMult - currentHealthMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostHp, boostHealthcount);

            if (Cruelty.rewardScaling != ScalingMode.None)
            {
                DeathRewards dr = characterBody.GetComponent<DeathRewards>();
                if (dr)
                {
                    float scaledXpRewards = (dr.expReward + dr.expReward * deathRewardsAdd) * deathRewardsMultiplier;
                    float scaledGoldRewards = (dr.goldReward + dr.goldReward * deathRewardsAdd) * deathRewardsMultiplier;

                    uint scaledXpFloor = (uint)Mathf.CeilToInt(scaledXpRewards);
                    uint scaledGoldFloor = (uint)Mathf.CeilToInt(scaledGoldRewards);

                    if (scaledXpFloor > dr.expReward) dr.expReward = scaledXpFloor;
                    if (scaledGoldFloor > dr.goldReward) dr.goldReward = scaledGoldFloor;
                }
            }

            return currentDirectorCredits - availableCredits;
        }
    }
}
