﻿using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Risky_Artifacts.Artifacts
{
    public class Hunted
    {
        public static HashSet<BodyIndex> TurretNerfList = new HashSet<BodyIndex>();
        public static List<CharacterSpawnCard> SpawnCards = new List<CharacterSpawnCard>();
        public static List<DirectorCard> DirectorCards = new List<DirectorCard>();
        private List<SpawnInfo> SpawnInfoList = new List<SpawnInfo>();
        public static string spawnInfoInput;
        public static bool allSurvivors = false;
        public static bool nerfEngiTurrets = true;
        public static bool nerfPercentHeal = true;
        public static bool useOverlay = true;

        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static ItemDef HuntedStatItem;

        public static float healthMult = 10f;
        public static float damageMult = 0.2f;
        public static int directorCost = 10;

        public Hunted()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfHunted";
            artifact.nameToken = "RISKYARTIFACTS_HUNTED_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_HUNTED_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginDisabled.png");//todo
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginEnabledClean.png");//todo
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            CreateHuntedItem();
            ParseSpawnlist();
            RoR2.RoR2Application.onLoad += OnLoad;

            ArtifactHooks();
        }

        private void ParseSpawnlist()
        {
            spawnInfoInput = new string(spawnInfoInput.ToCharArray().Where(c => !System.Char.IsWhiteSpace(c)).ToArray());
            string[] splitStages = spawnInfoInput.Split(',');
            foreach (string str in splitStages)
            {
                string[] current = str.Split(':');

                string name = current[0];
                int cost = Hunted.directorCost;
                if (current.Length > 1 && int.TryParse(current[1], out int parsedCost))
                {
                    cost = parsedCost;
                }
                SpawnInfoList.Add(new SpawnInfo()
                {
                    bodyName = name,
                    directorCost = cost
                });
            }
        }
        
        private void OnLoad()
        {
            TurretNerfList.Add(BodyCatalog.FindBodyIndex("EngiBeamTurretBody"));
            TurretNerfList.Add(BodyCatalog.FindBodyIndex("EngiWalkerTurretBody"));
            TurretNerfList.Add(BodyCatalog.FindBodyIndex("EngiTurretBody"));

            //This will be used if AllSurvivors is true
            HashSet<BodyIndex> usedBodyIndices = new HashSet<BodyIndex>();

            SpawnInfo[] infoArray = SpawnInfoList.ToArray();
            for (int i = 0; i < infoArray.Length; i++)
            {
                infoArray[i].bodyIndex = BodyCatalog.FindBodyIndexCaseInsensitive(infoArray[i].bodyName);
            }
            SpawnInfoList = infoArray.Where(info => info.bodyIndex != BodyIndex.None).ToList();

            foreach (SpawnInfo info in SpawnInfoList)
            {
                usedBodyIndices.Add(info.bodyIndex);
            }

            if (allSurvivors)
            {
                foreach (SurvivorDef survivor in SurvivorCatalog.allSurvivorDefs)
                {
                    if (!survivor.bodyPrefab) continue;
                    CharacterBody body = survivor.bodyPrefab.GetComponent<CharacterBody>();
                    if (!body || body.bodyIndex == BodyIndex.None) continue;

                    if (!usedBodyIndices.Contains(body.bodyIndex))
                    {
                        usedBodyIndices.Add(body.bodyIndex);

                        SpawnInfoList.Add(new SpawnInfo()
                        {
                            bodyIndex = body.bodyIndex,
                            directorCost = Hunted.directorCost
                        });
                    }
                }
            }

            BuildSpawncards();
        }

        private void BuildSpawncards()
        {
            foreach (SpawnInfo info in SpawnInfoList)
            {
                Debug.Log("RiskyArtifacts: Creating card for " + info.bodyIndex + " - " + info.bodyName);
                GameObject master = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(info.bodyIndex));
                if (!master)
                {
                    Debug.LogError("RiskyArtifacts: Master could not be found for survivor " + info.bodyIndex + " - " + info.bodyName);
                    continue;
                }

                CharacterSpawnCard csc = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                csc.prefab = master;
                csc.directorCreditCost = info.directorCost;
                csc.eliteRules = SpawnCard.EliteRules.Default;
                csc.forbiddenAsBoss = false;
                csc.hullSize = HullClassification.Human;
                csc.noElites = false;
                csc.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                csc.itemsToGrant = new ItemCountPair[] { new ItemCountPair() { itemDef = Hunted.HuntedStatItem, count = 1 }};
                csc.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
                csc.occupyPosition = false;
                csc.loadout = new SerializableLoadout();
                csc.sendOverNetwork = true;
                csc.requiredFlags = RoR2.Navigation.NodeFlags.None;
                csc.name = "cscHunted" + info.bodyIndex;
                (csc as ScriptableObject).name = csc.name;
                SpawnCards.Add(csc);

                DirectorCard dc = new DirectorCard()
                {
                    selectionWeight = 1,
                    spawnCard = csc,
                    spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                    preventOverhead = false,
                    minimumStageCompletions = 0
                };
                DirectorCards.Add(dc);
            }
        }

        private void CreateHuntedItem()
        {
            HuntedStatItem = ScriptableObject.CreateInstance<ItemDef>();
            HuntedStatItem.name = "RiskyArtifactsHuntedStatItem";
            HuntedStatItem.deprecatedTier = ItemTier.NoTier;
            HuntedStatItem.nameToken = "RISKYARTIFACTS_HUNTEDSTATITEM_NAME";
            HuntedStatItem.pickupToken = "RISKYARTIFACTS_HUNTEDSTATITEM_PICKUP";
            HuntedStatItem.descriptionToken = "RISKYARTIFACTS_HUNTEDSTATITEM_DESC";
            HuntedStatItem.tags = new[]
            {
                ItemTag.WorldUnique,
                ItemTag.BrotherBlacklist,
                ItemTag.CannotSteal
            };
            ItemDisplayRule[] idr = new ItemDisplayRule[0];
            //ContentAddition.AddItemDef(OriginBonusItem);
            ItemAPI.Add(new CustomItem(HuntedStatItem, idr));

            if (useOverlay)
            {
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
                            toReturn += self.body.inventory.GetItemCount(HuntedStatItem);
                        }
                        return toReturn;
                    });
                };
            }

            On.RoR2.HealthComponent.TakeDamage += BlockFallDamageAndVoidDamage;
            On.RoR2.CharacterBody.RecalculateStats += HuntedStats;
            if (Hunted.nerfPercentHeal)On.RoR2.HealthComponent.HealFraction += ReduceHuntedPercentHeal;

            IL.RoR2.MapZone.TryZoneStart += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                     x => x.MatchLdcI4(0),
                     x => x.MatchStloc(3),
                     x => x.MatchLdloc(0)
                    ))
                {
                    c.Index++;
                    c.Emit(OpCodes.Ldloc, 0);   //CharacterBody
                    c.EmitDelegate<Func<bool, CharacterBody, bool>>((flag, body) =>
                    {
                        return flag || (body.inventory && body.inventory.GetItemCount(Hunted.HuntedStatItem) > 0);
                    });
                }
                else
                {
                    Debug.LogError("RiskyArtifacts: Hunted MapZone fall death prevention IL hook failed.");
                }
            };
        }

        private void BlockFallDamageAndVoidDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active)
            {
                if (self.body && self.body.inventory && self.body.inventory.GetItemCount(Hunted.HuntedStatItem) > 0)
                {

                    bool isVoidDamage = !damageInfo.attacker && !damageInfo.inflictor
                        && damageInfo.damageColorIndex == DamageColorIndex.Void
                        && damageInfo.damageType == (DamageType.BypassArmor | DamageType.BypassBlock);
                    if (damageInfo.damageType.HasFlag(DamageType.FallDamage) || isVoidDamage)
                    {
                        damageInfo.damage = 0f;
                        damageInfo.rejected = true;
                    }
                }
            }

            orig(self, damageInfo);
        }

        private float ReduceHuntedPercentHeal(On.RoR2.HealthComponent.orig_HealFraction orig, HealthComponent self, float fraction, ProcChainMask procChainMask)
        {
            if (self.body.inventory && self.body.inventory.GetItemCount(Hunted.HuntedStatItem) > 0) fraction /= Hunted.healthMult;
            return orig(self, fraction, procChainMask);
        }

        private void HuntedStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory && self.inventory.GetItemCount(Hunted.HuntedStatItem) > 0)
            {
                self.damage *= Hunted.damageMult;

                if (!(Hunted.nerfEngiTurrets && Hunted.TurretNerfList.Contains(self.bodyIndex)))
                {
                    self.maxHealth *= Hunted.healthMult;
                    self.maxShield *= Hunted.healthMult;
                }
            }
        }

        private void ArtifactHooks()
        {
            IL.RoR2.ClassicStageInfo.OnArtifactEnabled += (il) =>
            {
                //Cursed hook. Ldarg 2 = ArtifactDef added. It wants to rebuild cards if Dissonance/Kin are enabled, so trick it into thinking they are.
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After, x => x.MatchLdarg(2)))
                {
                    c.EmitDelegate<Func<ArtifactDef, ArtifactDef>>(artifact =>
                    {
                        if (artifact == Hunted.artifact) return RoR2Content.Artifacts.mixEnemyArtifactDef;
                        return artifact;
                    });
                }
                else
                {
                    Debug.Log("RiskyArtifacts: Hunted ClassicStageInfo.OnArtifactEnabled IL Hook failed.");
                }
            };

            IL.RoR2.ClassicStageInfo.OnArtifactDisabled += (il) =>
            {
                //Cursed hook. Ldarg 2 = ArtifactDef added. It wants to rebuild cards if Dissonance/Kin are enabled, so trick it into thinking they are.
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After, x => x.MatchLdarg(2)))
                {
                    c.EmitDelegate<Func<ArtifactDef, ArtifactDef>>(artifact =>
                    {
                        if (artifact == Hunted.artifact) return RoR2Content.Artifacts.mixEnemyArtifactDef;
                        return artifact;
                    });
                }
                else
                {
                    Debug.Log("RiskyArtifacts: Hunted ClassicStageInfo.OnArtifactDisabled IL Hook failed.");
                }
            };

            On.RoR2.ClassicStageInfo.RebuildCards += ClassicStageInfo_RebuildCards;
        }

        //This'll bypass other monster pool-modifying artifacts.
        private void ClassicStageInfo_RebuildCards(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self)
        {
            orig(self);

            if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(Hunted.artifact)) return;

            if (self.modifiableMonsterCategories != null)
            {
                foreach (DirectorCardCategorySelection.Category category in self.modifiableMonsterCategories.categories)
                {
                    if (category.name == "Minibosses")
                    {
                        AddToCategory(category, DirectorCards);
                        break;
                    }
                }
            }
            self.monsterSelection = self.modifiableMonsterCategories.GenerateDirectorCardWeightedSelection();
        }

        private static void AddToCategory(DirectorCardCategorySelection.Category category, List<DirectorCard> cardsToAdd)
        {
            List<DirectorCard> cardsInCategory = category.cards.ToList();
            cardsInCategory.AddRange(cardsToAdd);
            category.cards = cardsInCategory.ToArray();
        }

        //Used to construct spawncards when reading from config
        public class SpawnInfo
        {
            public string bodyName;
            public BodyIndex bodyIndex = BodyIndex.None;
            public int directorCost;
        }
    }
}
