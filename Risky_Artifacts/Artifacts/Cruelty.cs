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
    public class Cruelty
    {
        public static List<BodyIndex> RunEndBosses = new List<BodyIndex>();
        public static List<EliteDef> BlacklistedElites = new List<EliteDef>();
        public static ArtifactDef artifact;
        public static bool enabled = true;
        public static bool guaranteeRunEndBoss = true;
        public static ScalingMode damageScaling;
        public static ScalingMode healthScaling;
        public static ScalingMode costScaling;

        public static float failureChance = 25f;

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

            On.RoR2.CombatDirector.Awake +=  (orig, self) =>
            {
                orig(self);
                if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Cruelty.artifact))
                {
                    self.onSpawnedServer.AddListener(delegate (GameObject targetGameObject)
                    {
                        if (!NetworkServer.active) return;

                        DirectorCard lastAttemptedMonsterCard = self.lastAttemptedMonsterCard;
                        float monsterCredit = self.monsterCredit;

                        CharacterMaster master = targetGameObject.GetComponent<CharacterMaster>();
                        if (master && master.inventory && master.inventory.GetItemCount(RoR2Content.Items.HealthDecay) <= 0)
                        {
                            CharacterBody body = master.GetBody();
                            if (body &&
                            !body.isPlayerControlled
                            && !body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Masterless)
                            && ((body.isBoss || body.isChampion) || Random.Range(1, 100) <= 25) || (guaranteeRunEndBoss && RunEndBosses.Contains(body.bodyIndex)))
                                self.monsterCredit -= Cruelty.CreateCrueltyElite(body, master.inventory, monsterCredit, lastAttemptedMonsterCard.cost, Cruelty.failureChance);
                        }
                    });
                }
            };

            RoR2.RoR2Application.onLoad += OnLoad;
        }

        private void OnLoad()
        {
            EquipmentIndex blightIndex = EquipmentCatalog.FindEquipmentIndex("AffixBlightedMoffein");
            if (blightIndex != EquipmentIndex.None)
            {
                EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(blightIndex);
                if (ed && ed.passiveBuffDef && ed.passiveBuffDef.eliteDef) BlacklistedElites.Add(ed.passiveBuffDef.eliteDef);
            }

            RunEndBosses.Add(BodyCatalog.FindBodyIndex("BrotherBody"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabBody.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabJointBody.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyBase.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyPhase1.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyPhase2.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyPhase3.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/Base/ScavLunar/ScavLunar1Body.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/Base/ScavLunar/ScavLunar2Body.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/Base/ScavLunar/ScavLunar3Body.prefab"));
            RunEndBosses.Add(BodyCatalog.FindBodyIndex("RoR2/Base/ScavLunar/ScavLunar4Body.prefab"));
        }

        public static float CreateCrueltyElite(CharacterBody characterBody, Inventory inventory, float currentDirectorCredits, int cardCost, float failureChance)
        {
            if (!characterBody || !inventory) return 0f;
            float availableCredits = currentDirectorCredits;

            //Check amount of elite buffs the target has
            List<BuffIndex> currentEliteBuffs = new List<BuffIndex>();
            foreach (BuffIndex b in BuffCatalog.eliteBuffIndices)
            {
                if (characterBody.HasBuff(b) && !currentEliteBuffs.Contains(b)) currentEliteBuffs.Add(b);
            }
            int eliteBuffs = currentEliteBuffs.Count();

            bool hasEquip = inventory.GetEquipmentIndex() != EquipmentIndex.None;

            float currentHealthMult = 1f + inventory.GetItemCount(RoR2Content.Items.BoostHp) * 0.1f;
            float currentDamageMult = 1f + inventory.GetItemCount(RoR2Content.Items.BoostDamage) * 0.1f;

            float desiredHealthMult = currentHealthMult;
            float desiredDamageMult = currentDamageMult;

            List<EliteDef> selectedElites = new List<EliteDef>();

            //Roll for failure each time an affix is added.
            //if ((float)UnityEngine.Random.Range(0, 100) < Cruelty.failureChance) break;

            float totalCostMult = 1f;

            //Iterate through all elites, starting from the most expensive
            //Seems very inefficient
            List<CombatDirector.EliteTierDef> eliteTiersList = EliteAPI.GetCombatDirectorEliteTiers().ToList();
            eliteTiersList.Sort(Utils.CompareEliteTierCost);
            foreach (CombatDirector.EliteTierDef etd in eliteTiersList)
            {

                List<EliteDef> availableElitesInTier = etd.availableDefs.Where(x => !selectedElites.Contains(x) && !BlacklistedElites.Contains(x)).ToList();
                Utils.Shuffle(availableElitesInTier);
                foreach (EliteDef ed in availableElitesInTier)
                {
                    //Check if EliteDef has an associated buff and the character doesn't already have the buff.
                    bool isBuffValid = ed && ed.eliteEquipmentDef
                        && ed.eliteEquipmentDef.passiveBuffDef
                        && ed.eliteEquipmentDef.passiveBuffDef.isElite
                        && !currentEliteBuffs.Contains(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
                    if (!isBuffValid) continue;
                    currentEliteBuffs.Add(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);

                    //TODO: Figure out how director credits work
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

                    if (hasEnoughCredits && UnityEngine.Random.Range(0, 100) > Cruelty.failureChance)
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

                        //Fill in equipment slot if it isn't filled
                        if (!hasEquip && ed.eliteEquipmentDef)
                        {
                            inventory.SetEquipmentIndex(ed.eliteEquipmentDef.equipmentIndex);
                            hasEquip = true;
                        }

                        //Apply Elite Bonus
                        characterBody.AddBuff(ed.eliteEquipmentDef.passiveBuffDef.buffIndex);
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

            int boostDamagecount = Mathf.FloorToInt((desiredDamageMult - currentDamageMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostDamage, boostDamagecount);

            int boostHealthcount = Mathf.FloorToInt((desiredHealthMult - currentHealthMult) / 0.1f);
            inventory.GiveItem(RoR2Content.Items.BoostHp, boostHealthcount);

            return currentDirectorCredits - availableCredits;
        }
    }
}
