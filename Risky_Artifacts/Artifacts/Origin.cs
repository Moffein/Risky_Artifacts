using RoR2;
using R2API;
using UnityEngine;
using Risky_Artifacts.Artifacts.MonoBehaviours;
using System.Collections.Generic;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using static Risky_Artifacts.Artifacts.MonoBehaviours.OriginExtraDrops;

namespace Risky_Artifacts.Artifacts
{
    public class Origin
    {
        public static ArtifactDef artifact;
        public static ItemDef OriginBonusItem;

        public static bool impOnly = false;
        public static bool allowEliteWorms = false;
        public static float cooldownMult = 0.7f;
        public static float atkSpeedMult = 1.3f;
        public static float damageMult = 1.2f;
        public static float moveSpeedMult = 1.5f;

        private static List<SpawnCard> t1BossCards;
        private static List<SpawnCard> t2BossCards;
        private static List<SpawnCard> t3BossCards;
        private static SpawnCard impCard;
        private static SpawnCard wormCard;

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

        public static bool enableGrandparent = true;

        public Origin()
        {
            LanguageAPI.Add("RISKYARTIFACTS_ORIGIN_NAME", "Artifact of Origination");   //Prevent conflicting with Chen's Origin
            LanguageAPI.Add("RISKYARTIFACTS_ORIGIN_DESC", impOnly ? "Imp Overlords" : "Boss monsters" + " invade the map every 10 minutes.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfOrigination";
            artifact.nameToken = "RISKYARTIFACTS_ORIGIN_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_ORIGIN_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texOriginDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texOriginEnabled.png");
            ArtifactAPI.Add(artifact);

            CreateOriginItem();

            OriginInvasionManager.Init();

            PopulateBossSpawncards();
        }

        private SpawnCard LoadSpawncard(string path)
        {
            return Resources.Load<SpawnCard>("spawncards/characterspawncards/" + path);
        }

        private void CreateOriginItem()
        {
            LanguageAPI.Add("RISKYARTIFACTS_ORIGINBONUSITEM_NAME", "Origin Bonus");
            LanguageAPI.Add("RISKYARTIFACTS_ORIGINBONUSITEM_PICKUP", "The party starts here.");
            LanguageAPI.Add("RISKYARTIFACTS_ORIGINBONUSITEM_DESC", "Increase <style=cIsUtility>movement speed</style>, <style=cIsDamage>attack speed</style> and <style=cIsDamage>damage</style>, and <style=cIsUtility>reduce skill cooldowns</style>.");

            LanguageAPI.Add("RISKYARTIFACTS_ORIGIN_SUBTITLENAMETOKEN", "Reclaimer");
            LanguageAPI.Add("RISKYARTIFACTS_ORIGIN_MODIFIER", "Vanguard");

            OriginBonusItem = ScriptableObject.CreateInstance<ItemDef>();
            OriginBonusItem.name = "RiskyArtifactsOriginBonus";
            OriginBonusItem.tier = ItemTier.NoTier;
            OriginBonusItem.nameToken = "RISKYARTIFACTS_ORIGINBONUSITEM_NAME";
            OriginBonusItem.pickupToken = "RISKYARTIFACTS_ORIGINBONUSITEM_PICKUP";
            OriginBonusItem.descriptionToken = "RISKYARTIFACTS_ORIGINBONUSITEM_DESC";
            OriginBonusItem.tags = new[]
            {
                ItemTag.WorldUnique
            };
            ItemDisplayRule[] idr = null;
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

            On.RoR2.Util.GetBestBodyName += (orig, bodyObject) =>
            {
                string toReturn = orig(bodyObject);
                CharacterBody cb = bodyObject.GetComponent<CharacterBody>();
                if (cb && cb.inventory && cb.inventory.GetItemCount(OriginBonusItem) > 0)
                {
                    toReturn += " " + Language.GetString("RISKYARTIFACTS_ORIGIN_MODIFIER"); ;
                }
                return toReturn;
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
                        return vengeanceCount + self.inventory.GetItemCount(OriginBonusItem);
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
                    return vengeanceCount + self.body.inventory.GetItemCount(OriginBonusItem);
                });
            };
        }

        public static void AddSpawnCard(SpawnCard spawnCard, BossTier tier)
        {
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

        public static SpawnCard SelectSpawnCard(Xoroshiro128Plus rng)
        {
            if (impOnly)
            {
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

                if (CombatDirector.IsEliteOnlyArtifactActive() && !allowEliteWorms)
                {
                    availableBosses.Remove(wormCard);
                }

                if (availableBosses.Count > 0)
                {
                    return availableBosses[rng.RangeInt(0, availableBosses.Count)];
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
                t1BossCards = new List<SpawnCard>();
                t2BossCards = new List<SpawnCard>();
                t3BossCards = new List<SpawnCard>();

                if (enableBeetle) t1BossCards.Add(LoadSpawncard("cscBeetleQueen")); //too useless
                if (enableVagrant) t1BossCards.Add(LoadSpawncard("cscVagrant"));
                if (enableTitan) t1BossCards.Add(LoadSpawncard("titan/cscTitanGooLake"));
                if (enableDunestrider) t1BossCards.Add(LoadSpawncard("cscClayBoss"));

                if (enableGrovetender) t2BossCards.Add(LoadSpawncard("cscGravekeeper"));
                if (enableRoboBall) t2BossCards.Add(LoadSpawncard("cscRoboBallBoss"));
                if (enableWorm) t2BossCards.Add(wormCard);
                if (enableImp) t2BossCards.Add(impCard);

                t3BossCards.Add(LoadSpawncard("titan/cscGrandparent"));
            }
        }

        public enum BossTier
        {
            t1, //Stage 1-2
            t2, //Stage 3-4
            t3  //Stage 5
        }

        public static void DropItem(Vector3 position, Xoroshiro128Plus treasureRng, EliteTier tier)
        {
            List<PickupIndex> list;

            float whiteChance = (tier == EliteTier.None || (tier == EliteTier.T1 && CombatDirector.IsEliteOnlyArtifactActive() && !Origin.ignoreHonor)) ? 50f : 0f;
            float greenChance = tier < EliteTier.T2 ? 35f : 0f;
            float redChance = 5f;
            float yellowChance = 10f;

            float total = whiteChance + greenChance + redChance + yellowChance;
            
            if (treasureRng.RangeFloat(0f, total) <= whiteChance)//drop white
            {
                list = Run.instance.availableTier1DropList;
            }
            else
            {
                total -= whiteChance;
                if (treasureRng.RangeFloat(0f, total) <= greenChance)//drop green
                {
                    list = Run.instance.availableTier2DropList;
                }
                else
                {
                    total -= greenChance;
                    if (treasureRng.RangeFloat(0f, total) <= redChance)//drop red
                    {
                        list = Run.instance.availableTier3DropList;
                    }
                    else
                    {
                        //There's probably a better way of doing this.
                        PickupIndex pearlIndex;
                        PickupIndex shinyPearlIndex;

                        list = new List<PickupIndex>();
                        pearlIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.Pearl.itemIndex);
                        bool pearlAvailable = pearlIndex != PickupIndex.none && Run.instance.IsItemAvailable(RoR2Content.Items.Pearl.itemIndex);

                        shinyPearlIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex);
                        bool shinyPearlAvailable = shinyPearlIndex != PickupIndex.none && Run.instance.IsItemAvailable(RoR2Content.Items.ShinyPearl.itemIndex);

                        if (pearlAvailable || shinyPearlAvailable)
                        {
                            if (pearlAvailable && shinyPearlAvailable)
                            {
                                if (tier < EliteTier.T1 && treasureRng.RangeFloat(0f, 100f) <= 80f)
                                {
                                    list.Add(pearlIndex);
                                }
                                else
                                {
                                    list.Add(shinyPearlIndex);
                                }
                            }
                            else
                            {
                                if (pearlAvailable) list.Add(pearlIndex);
                                if (shinyPearlAvailable ) list.Add(shinyPearlIndex);
                            }
                        }
                        else
                        {
                            if (tier < EliteTier.T2)
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
