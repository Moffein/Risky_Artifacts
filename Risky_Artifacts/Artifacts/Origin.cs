using RoR2;
using R2API;
using UnityEngine;
using Risky_Artifacts.Artifacts.MonoBehaviours;
using System.Collections.Generic;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using static Risky_Artifacts.Artifacts.MonoBehaviours.OriginExtraDrops;
using System.Linq;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;

namespace Risky_Artifacts.Artifacts
{
    public class Origin
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static ItemDef OriginBonusItem;

        public static bool impOnly = false;
        public static bool allowEliteWorms = false;
        public static float cooldownMult = 0.7f;
        public static float atkSpeedMult = 1.3f;
        public static float damageMult = 1.2f;
        public static float moveSpeedMult = 1.5f;
        public static float extraBossesPerInvasion = 1f;

        private static List<SpawnCard> t1BossCards;
        private static List<SpawnCard> t2BossCards;

        private static ExpansionDef dlc1Def = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

        private static List<SpawnCard> t2BossCards_DLC1;

        private static List<SpawnCard> t3BossCards;
        public static SpawnCard impCard;
        public static SpawnCard wormCard;

        public static bool useAdaptiveArmor = false;

        public static bool combineSpawns = true;
        public static bool disableParticles = false;
        public static bool ignoreHonor = false;

        public static bool enableTitan = true;
        public static bool enableVagrant = true;
        public static bool enableDunestrider = true;
        public static bool enableBeetle = false;

        public static bool enableImp = true;
        public static bool enableRoboBall = true;
        public static bool enableGrovetender = true;
        public static bool enableWorm = false;

        public static bool enableXi_DLC1 = true;
        public static bool enableVoidCrab_DLC1 = true;

        public static bool enableGrandparent = true;

        public static bool bossVoidTeam = true;

        public Origin()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfOrigination";
            artifact.nameToken = "RISKYARTIFACTS_ORIGIN_NAME";
            artifact.descriptionToken = !impOnly ? "RISKYARTIFACTS_ORIGIN_DESC" : "RISKYARTIFACTS_ORIGIN_DESC_IMPONLY";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginEnabledClean.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            CreateOriginItem();

            OriginInvasionManager.Init();

            PopulateBossSpawncards();
        }

        private SpawnCard LoadSpawncard(string path)
        {
            return LegacyResourcesAPI.Load<SpawnCard>("spawncards/characterspawncards/" + path);
        }

        private void CreateOriginItem()
        {
            OriginBonusItem = ScriptableObject.CreateInstance<ItemDef>();
            OriginBonusItem.name = "RiskyArtifactsOriginBonus";
            OriginBonusItem.deprecatedTier = ItemTier.NoTier;
            OriginBonusItem.nameToken = "RISKYARTIFACTS_ORIGINBONUSITEM_NAME";
            OriginBonusItem.pickupToken = "RISKYARTIFACTS_ORIGINBONUSITEM_PICKUP";
            OriginBonusItem.descriptionToken = "RISKYARTIFACTS_ORIGINBONUSITEM_DESC";
            OriginBonusItem.tags = new[]
            {
                ItemTag.WorldUnique,
                ItemTag.BrotherBlacklist,
                ItemTag.CannotSteal
            };
            ItemDisplayRule[] idr = new ItemDisplayRule[0];
            //ContentAddition.AddItemDef(OriginBonusItem);
            ItemAPI.Add(new CustomItem(OriginBonusItem, idr));

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender.inventory && sender.inventory.GetItemCount(OriginBonusItem) > 0)
                {
                    args.damageMultAdd += damageMult - 1f;
                    args.attackSpeedMultAdd += atkSpeedMult - 1f;
                    if (moveSpeedMult >= 1f)
                    {
                        args.moveSpeedMultAdd += moveSpeedMult - 1f;
                    }
                    else
                    {
                        args.moveSpeedReductionMultAdd += 1f - moveSpeedMult;
                    }    
                }
            };

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self.inventory && self.inventory.GetItemCount(OriginBonusItem) > 0)
                {
                    if (self.skillLocator)
                    {
                        if (self.skillLocator.primary)
                        {
                            self.skillLocator.primary.cooldownScale *= cooldownMult;
                        }
                        if (self.skillLocator.secondary)
                        {
                            self.skillLocator.secondary.cooldownScale *= cooldownMult;
                        }
                        if (self.skillLocator.utility)
                        {
                            self.skillLocator.utility.cooldownScale *= cooldownMult;
                        }
                        if (self.skillLocator.special)
                        {
                            self.skillLocator.special.cooldownScale *= cooldownMult;
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.GetSubtitle += (orig, self) =>
            {
                if (self.inventory && self.inventory.GetItemCount(OriginBonusItem) > 0)
                {
                    return Language.GetString("RISKYARTIFACTS_ORIGIN_SUBTITLENAMETOKEN");
                }
                return orig(self);
            };

            if (!disableParticles)
            {
                IL.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.GotoNext(
                         x => x.MatchLdsfld(typeof(RoR2Content.Items), "InvadingDoppelganger")
                        );
                    c.Index += 2;
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<int, CharacterBody, int>>((vengeanceCount, self) =>
                    {
                        int toReturn = vengeanceCount;
                        if (self.inventory)
                        {
                            toReturn += self.inventory.GetItemCount(OriginBonusItem);
                        }
                        return toReturn;
                    });
                };
            }

            IL.RoR2.CharacterModel.UpdateOverlays += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Items), "InvadingDoppelganger")
                    );
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, CharacterModel, int>>((vengeanceCount, self) =>
                {
                    int toReturn = vengeanceCount;
                    if (self.body && self.body.inventory)
                    {
                        toReturn += self.body.inventory.GetItemCount(OriginBonusItem);
                    }
                    return toReturn;
                });
            };
        }

        public static void AddSpawnCard(SpawnCard spawnCard, BossTier tier)
        {
            if (t1BossCards == null) t1BossCards = new List<SpawnCard>();
            if (t2BossCards == null) t2BossCards = new List<SpawnCard>();
            if (t2BossCards_DLC1 == null) t2BossCards_DLC1 = new List<SpawnCard>();
            if (t3BossCards == null) t3BossCards = new List<SpawnCard>();

            if (!impOnly)
            {
                switch (tier)
                {
                    case BossTier.t1:
                        t1BossCards.Add(spawnCard);
                        break;
                    case BossTier.t2:
                        t2BossCards.Add(spawnCard);
                        break;
                    case BossTier.t3:
                        t3BossCards.Add(spawnCard);
                        break;
                    default:
                        break;
                }
            }
        }

        public static SpawnCard SelectSpawnCard(Xoroshiro128Plus rng, ref SpawnCard prevBoss)
        {
            if (impOnly)
            {
                prevBoss = impCard;
                return impCard;
            }
            else
            {
                //This is super hard-coded. Weighted selection would be better if I could figure out how to set it up.
                bool t1Available = t1BossCards.Count > 0;
                bool t2Available = t2BossCards.Count > 0;
                bool t3Available = t3BossCards.Count > 0;

                List<SpawnCard> availableBosses = null;
                int stageNumber = Run.instance.stageClearCount;
                if (stageNumber < 2)
                {
                    if (t1Available)
                    {
                        availableBosses = t1BossCards;
                    }
                    else if (t2Available)
                    {
                        availableBosses = t2BossCards;
                    }
                    else if (t3Available)
                    {
                        availableBosses = t3BossCards;
                    }
                }
                else if (stageNumber < 4)
                {
                    if (t1Available && rng.RangeInt(1,11) < 4)
                    {
                        availableBosses = t1BossCards;
                    }
                    else
                    {
                        if (t2Available)
                        {
                            availableBosses = t2BossCards;
                        }
                        else if (t3Available)
                        {
                            availableBosses = t3BossCards;
                        }
                    }
                }
                else
                {
                    int roll = rng.RangeInt(10, 100);
                    if (t1Available && roll < 25)
                    {
                        availableBosses = t1BossCards;
                    }
                    else if (t2Available && (!t1Available || roll < 90))
                    {
                        availableBosses = t2BossCards;
                    }
                    else if (t3Available)
                    {
                        availableBosses = t3BossCards;
                    }
                }

                //Add DLC1 Bosses
                if (availableBosses == t2BossCards)
                {
                    if (Run.instance.IsExpansionEnabled(dlc1Def))
                    {
                        List<SpawnCard> temp = new List<SpawnCard>(availableBosses.Count + t2BossCards_DLC1.Count);
                        temp.AddRange(availableBosses);
                        temp.AddRange(t2BossCards_DLC1);
                        availableBosses = temp;
                    }
                }

                if (CombatDirector.IsEliteOnlyArtifactActive() && !allowEliteWorms)
                {
                    availableBosses.Remove(wormCard);
                }

                //Attempt to filter out previous boss
                if (prevBoss != null)
                {
                    if (availableBosses.Contains(prevBoss) && availableBosses.Count - 1 > 0)
                    {
                        SpawnCard[] temp = new SpawnCard[availableBosses.Count];
                        availableBosses.CopyTo(temp, 0);
                        availableBosses = temp.ToList<SpawnCard>();
                        availableBosses.Remove(prevBoss);
                    }
                }

                if (availableBosses.Count > 0)
                {
                    SpawnCard selectedBoss = availableBosses[rng.RangeInt(0, availableBosses.Count)];
                    prevBoss = selectedBoss;
                    return selectedBoss;
                }
                else
                {
                    Debug.LogError("RiskyArtifacts: Could not pick Origin Spawncard");
                    return null;
                }
            }
        }
        private void PopulateBossSpawncards()
        {
            impCard = LoadSpawncard("cscImpBoss");
            wormCard = LoadSpawncard("cscMagmaWorm");
            if (!impOnly)
            {
                if (t1BossCards == null) t1BossCards = new List<SpawnCard>();
                if (t2BossCards == null) t2BossCards = new List<SpawnCard>();
                if (t2BossCards_DLC1 == null) t2BossCards_DLC1 = new List<SpawnCard>();
                if (t3BossCards == null) t3BossCards = new List<SpawnCard>();

                if (enableBeetle) t1BossCards.Add(LoadSpawncard("cscBeetleQueen")); //too useless
                if (enableVagrant) t1BossCards.Add(LoadSpawncard("cscVagrant"));
                if (enableTitan) t1BossCards.Add(LoadSpawncard("titan/cscTitanGooLake"));
                if (enableDunestrider) t1BossCards.Add(LoadSpawncard("cscClayBoss"));

                if (enableGrovetender) t2BossCards.Add(LoadSpawncard("cscGravekeeper"));
                if (enableRoboBall) t2BossCards.Add(LoadSpawncard("cscRoboBallBoss"));
                if (enableWorm) t2BossCards.Add(wormCard);
                if (enableImp) t2BossCards.Add(impCard);

                if (enableXi_DLC1) t2BossCards_DLC1.Add(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset").WaitForCompletion());
                if (enableXi_DLC1) t2BossCards_DLC1.Add(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset").WaitForCompletion());

                if (enableGrandparent) t3BossCards.Add(LoadSpawncard("titan/cscGrandparent"));
            }
        }

        public enum BossTier
        {
            t1, //Stage 1-2
            t2, //Stage 3-4
            t3  //Stage 5
        }

        public static void DropItem(Vector3 position, Xoroshiro128Plus treasureRng, float cost, GameObject victimObject, PickupIndex bossDrop)
        {
            List<PickupIndex> list;

            float whiteChance = 50f;
            float greenChance = 35f;
            float redChance = 5f;

            whiteChance = Mathf.Lerp(whiteChance, 0f, (cost - 1f) / 2f);   //3 whites = 1 guaranteed green
            greenChance = cost < 3f ? greenChance : Mathf.Lerp(greenChance, 0f, (cost-3f)/12f); //15 whites = 1 guaranteed red

            float yellowChance = (whiteChance + greenChance) > 0 ? (whiteChance + greenChance + redChance) * 0.08f : 0f;


            float total = whiteChance + greenChance + redChance + yellowChance;

            if (victimObject)
            {
                position = (SneedUtils.SneedUtils.FindSafeTeleportPosition(victimObject, position));
            }    

            if (treasureRng.RangeFloat(0f, total) <= whiteChance)//drop white
            {
                list = Run.instance.availableTier1DropList.Concat(Run.instance.availableVoidTier1DropList).ToList();
            }
            else
            {
                total -= whiteChance;
                if (treasureRng.RangeFloat(0f, total) <= greenChance)//drop green
                {
                    list = Run.instance.availableTier2DropList.Concat(Run.instance.availableVoidTier2DropList).ToList();
                }
                else
                {
                    total -= greenChance;
                    if (treasureRng.RangeFloat(0f, total) <= redChance)//drop red
                    {
                        list = Run.instance.availableTier3DropList.Concat(Run.instance.availableVoidTier3DropList).ToList();
                    }
                    else//drop yellow
                    {
                        if (bossDrop != PickupIndex.none)
                        {
                            list = new List<PickupIndex>() { bossDrop };
                        }
                        else
                        {
                            if (cost < 15f)
                            {
                                list = Run.instance.availableTier2DropList;
                            }
                            else
                            {
                                list = Run.instance.availableTier3DropList;
                            }
                        }
                    }
                }
            }
            int index = treasureRng.RangeInt(0, list.Count);
            PickupDropletController.CreatePickupDroplet(list[index], position, Vector3.up * 20f);
        }
    }
}
