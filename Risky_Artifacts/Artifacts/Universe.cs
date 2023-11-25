using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Timeline;

namespace Risky_Artifacts.Artifacts
{
    public class Universe
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static DirectorCardCategorySelection MonsterCardSelection;
        public static ItemDef UniverseScriptedEncounterStatItem;

        public static class InputInfo
        {
            public static string Basic_Monsters, Minibosses, Champions, Special;
        }

        public static CategoryInfo CatBasicMonsters = new CategoryInfo()
        {
            weight = 3f,
            category = MonsterCategory.Basic_Monsters
        };

        public static CategoryInfo CatMinibosses = new CategoryInfo()
        {
            weight = 2f,
            category = MonsterCategory.Minibosses
        };

        public static CategoryInfo CatChampions = new CategoryInfo()
        {
            weight = 2f,
            category = MonsterCategory.Champions
        };

        public static CategoryInfo CatSpecial = new CategoryInfo()
        {
            weight = 1f,
            category = MonsterCategory.Special
        };

        public static CategoryInfo CatLunarScav = new CategoryInfo()
        {
            weight = 0f,
            category = MonsterCategory.LunarScav
        };


        public static CategoryInfo CatVoidling = new CategoryInfo()
        {
            weight = 0f,
            category = MonsterCategory.LunarScav
        };

        public class CategoryInfo
        {
            public MonsterCategory category;
            public float weight;
            public List<DirectorCard> cards;
        }

        public class DirectorInfo
        {
            public string bodyName;
            public BodyIndex bodyIndex;
            public MonsterCategory monsterCategory = MonsterCategory.Basic_Monsters;
            public int directorCost = -1;
        }

        public enum MonsterCategory
        {
            Basic_Monsters, Minibosses, Champions, Special, LunarScav, Mithrix, MithrixHurt, Voidling, Drone, Newt
        }

        public Universe()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfUniverse";
            artifact.nameToken = "RISKYARTIFACTS_UNIVERSE_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_UNIVERSE_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedDisabled.png");//todo
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedEnabled.png");//todo
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            Universe_Spawncards.Init();
            RoR2Application.onLoad += OnLoad;
            ArtifactHooks();
            BuildItem();
        }

        private void OnLoad()
        {
            CatBasicMonsters.cards = ParseSpawnlist(Universe.InputInfo.Basic_Monsters, MonsterCategory.Basic_Monsters);
            CatMinibosses.cards = ParseSpawnlist(Universe.InputInfo.Minibosses, MonsterCategory.Minibosses);
            CatChampions.cards = ParseSpawnlist(Universe.InputInfo.Champions, MonsterCategory.Champions);
            CatSpecial.cards = ParseSpawnlist(Universe.InputInfo.Special, MonsterCategory.Special);

            BuildCatMithrix();

            MonsterCardSelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();

            if (CatBasicMonsters.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Basic Monsters", CatBasicMonsters.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = CatBasicMonsters.cards.ToArray();
            }

            if (CatMinibosses.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Minibosses", CatMinibosses.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = CatMinibosses.cards.ToArray();
            }

            if (CatChampions.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Champions", CatChampions.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = CatChampions.cards.ToArray();
            }

            if (CatSpecial.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Special", CatSpecial.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = CatSpecial.cards.ToArray();
            }
        }

        //Brother card isn't added to the 
        private void BuildCatMithrix()
        {
            CharacterSpawnCard itBrotherCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/cscBrotherIT.asset").WaitForCompletion();

            Debug.Log("RiskyArtifacts: Dumping Mithrix Stats");
            Debug.Log("Director Cost: " + itBrotherCard.directorCreditCost);
            foreach (ItemCountPair pair in itBrotherCard.itemsToGrant)
            {
                Debug.Log(pair.itemDef + " - " + pair.count);
            }
            Debug.Log("Hull Size: " + itBrotherCard.hullSize);

            CharacterSpawnCard brotherCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
        }

        private void ArtifactHooks()
        {
            On.RoR2.ClassicStageInfo.OnArtifactEnabled += ClassicStageInfo_OnArtifactEnabled;
            On.RoR2.ClassicStageInfo.OnArtifactDisabled += ClassicStageInfo_OnArtifactDisabled;

            IL.RoR2.ClassicStageInfo.RebuildCards += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchStfld(typeof(ClassicStageInfo), "modifiableMonsterCategories")))
                {
                    c.EmitDelegate<Func<DirectorCardCategorySelection, DirectorCardCategorySelection>>(dccs =>
                    {
                        if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Universe.artifact)) return Universe.MonsterCardSelection;
                        return dccs;
                    });
                }
                else
                {
                    Debug.Log("RiskyArtifacts: Universe RebuildCards IL Hook failed.");
                }
            };
        }

        private void BuildItem()
        {
            UniverseScriptedEncounterStatItem = ScriptableObject.CreateInstance<ItemDef>();
            UniverseScriptedEncounterStatItem.name = "RiskyArtifactsUniverseScriptedEncounterStatItem";
            UniverseScriptedEncounterStatItem.deprecatedTier = ItemTier.NoTier;
            UniverseScriptedEncounterStatItem.nameToken = "RISKYARTIFACTS_UNIVERSESCRIPTEDENCOUNTERSTATITEM_NAME";
            UniverseScriptedEncounterStatItem.pickupToken = "RISKYARTIFACTS_UNIVERSESCRIPTEDENCOUNTERSTATITEM_PICKUP";
            UniverseScriptedEncounterStatItem.descriptionToken = "RISKYARTIFACTS_UNIVERSESCRIPTEDENCOUNTERSTATITEM_DESC";
            UniverseScriptedEncounterStatItem.tags = new[]
            {
                ItemTag.WorldUnique,
                ItemTag.BrotherBlacklist,
                ItemTag.CannotSteal
            };
            ItemDisplayRule[] idr = new ItemDisplayRule[0];
            //ContentAddition.AddItemDef(OriginBonusItem);
            ItemAPI.Add(new CustomItem(UniverseScriptedEncounterStatItem, idr));

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.inventory && sender.inventory.GetItemCount(Universe.UniverseScriptedEncounterStatItem) > 0)
            {
                float healthFactor = 1f;
                float damageFactor = 1f;
                healthFactor += Run.instance.difficultyCoefficient / 2.5f;
                damageFactor += Run.instance.difficultyCoefficient / 30f;
                int playerFactor = Mathf.Max(1, Run.instance.livingPlayerCount);
                healthFactor *= Mathf.Pow((float)playerFactor, 0.5f);

                args.healthMultAdd += healthFactor - 1f;
                args.damageMultAdd = damageFactor - 1f;
            }
        }

        private void ClassicStageInfo_OnArtifactEnabled(On.RoR2.ClassicStageInfo.orig_OnArtifactEnabled orig, ClassicStageInfo self, RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            orig(self, runArtifactManager, artifactDef);
            if (artifactDef == Universe.artifact) self.RebuildCards();
        }

        private void ClassicStageInfo_OnArtifactDisabled(On.RoR2.ClassicStageInfo.orig_OnArtifactDisabled orig, ClassicStageInfo self, RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            orig(self, runArtifactManager, artifactDef);
            if (artifactDef == Universe.artifact) self.RebuildCards();
        }

        //Category is used for cost estimation
        public static List<DirectorCard> ParseSpawnlist(string inputString, MonsterCategory monsterCategory)
        {
            List<DirectorCard> cardList = new List<DirectorCard>();
            inputString = new string(inputString.ToCharArray().Where(c => !System.Char.IsWhiteSpace(c)).ToArray());
            string[] splitStages = inputString.Split(',');
            foreach (string str in splitStages)
            {
                string[] current = str.Split(':');

                string name = current[0];

                Universe_Spawncards.CardDict.TryGetValue(name, out CharacterSpawnCard origSpawnCard);
                int cost = origSpawnCard ? origSpawnCard.directorCreditCost : -1;
                int minStages = 0;

                if (current.Length > 1)
                {
                    for (int i = 1; i < current.Length; i++)
                    {
                        switch (i)
                        {
                            case 1:
                                if (int.TryParse(current[1], out int parsedCost))
                                {
                                    cost = parsedCost;
                                    if (origSpawnCard) origSpawnCard.directorCreditCost = parsedCost;
                                }
                                break;
                            case 2:
                                if (int.TryParse(current[2], out int parsedMinStages))
                                {
                                    minStages = parsedMinStages;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                //Get Master
                GameObject masterPrefab = null;
                BodyIndex index = BodyIndex.None;
                if (!origSpawnCard)
                {
                    Debug.LogWarning("RiskyArtifacts: Universe: Could not find spawncard for " + name + ".");
                    index = BodyCatalog.FindBodyIndex(name);
                    if (index != BodyIndex.None)
                    {
                        masterPrefab = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(index));
                    }
                }
                else
                {
                    masterPrefab = origSpawnCard.prefab;
                }

                //Get Body
                if (!masterPrefab)
                {
                    Debug.LogError("RiskyArtifacts: Universe: Could not create spawncard for " + name + ", AI Master not found.");
                    continue;
                }
                else
                {
                    if (index == BodyIndex.None)
                    {
                        CharacterMaster master = masterPrefab.GetComponent<CharacterMaster>();
                        if (master && master.bodyPrefab)
                        {
                            CharacterBody masterBody = master.bodyPrefab.GetComponent<CharacterBody>();
                            if (masterBody)
                            {
                                index = masterBody.bodyIndex;
                            }
                        }
                    }
                }
                GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(index);
                CharacterBody body = bodyPrefab ? bodyPrefab.GetComponent<CharacterBody>() : null;
                if (!body)
                {
                    Debug.LogError("RiskyArtifacts: Universe: Could not create spawncard for " + name + ", Body not found.");
                    continue;
                }

                //Build new card
                CharacterSpawnCard newCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                newCard.prefab = masterPrefab;
                newCard.directorCreditCost = cost;
                if (origSpawnCard)
                {
                    newCard.eliteRules = origSpawnCard.eliteRules;
                    newCard.forbiddenAsBoss = origSpawnCard.forbiddenAsBoss;
                    newCard.hullSize = origSpawnCard.hullSize;
                    newCard.noElites = origSpawnCard.noElites;
                    newCard.nodeGraphType = origSpawnCard.nodeGraphType;
                    newCard.forbiddenFlags = origSpawnCard.forbiddenFlags;
                    newCard.occupyPosition = origSpawnCard.occupyPosition;
                    newCard.loadout = origSpawnCard.loadout;
                    newCard.requiredFlags = origSpawnCard.requiredFlags;
                }
                else
                {
                    newCard.eliteRules = SpawnCard.EliteRules.Default;
                    newCard.forbiddenAsBoss = false;
                    newCard.hullSize = body.hullClassification;
                    newCard.noElites = false;
                    newCard.nodeGraphType = body.isFlying ? RoR2.Navigation.MapNodeGroup.GraphType.Air : RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                    newCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
                    newCard.occupyPosition = false;
                    newCard.loadout = new SerializableLoadout();
                    newCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
                }
                newCard.sendOverNetwork = true;
                newCard.name = "cscUniverse" + name;
                (newCard as ScriptableObject).name = newCard.name;

                //Estimate Cost
                if (newCard.directorCreditCost < 0)
                {
                    switch (monsterCategory)
                    {
                        case MonsterCategory.Special:
                            newCard.directorCreditCost = 4000;
                            break;
                        case MonsterCategory.Champions:
                            newCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / 3.5f);
                            break;
                        case MonsterCategory.Minibosses:
                            newCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / (body.isFlying ? 3.5f : 7f));
                            break;
                        case MonsterCategory.Basic_Monsters:
                        default:
                            newCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / (body.isFlying ? 3.5f : 7.5f));
                            break;
                    }
                }

                bool cardExists = Universe_Spawncards.CardDict.ContainsKey(name);
                if (cardExists)
                {
                    Debug.Log("RiskyArtifacts: Universe: Overwriting spawncard for " + name + ".");
                    Universe_Spawncards.CardDict[name] = newCard;
                }
                else
                {
                    Universe_Spawncards.CardDict.Add(name, newCard);
                }
                Debug.Log("RiskyArtifacts: Universe: Created spawncard for " + name + " with cost " + newCard.directorCreditCost + ".");

                if (monsterCategory == MonsterCategory.Special)
                {
                    if (newCard.itemsToGrant == null) newCard.itemsToGrant = new ItemCountPair[0];
                    List<ItemCountPair> itemList = newCard.itemsToGrant.ToList();
                    itemList.Add(new ItemCountPair() { count = 1,
                    itemDef = Universe.UniverseScriptedEncounterStatItem});
                    newCard.itemsToGrant = itemList.ToArray();
                }

                //Jellyfish thing is jank, but it's the only monster that actually uses this spawndistance.
                DirectorCard dc = new DirectorCard()
                {
                    selectionWeight = 1,
                    spawnCard = newCard,
                    spawnDistance = name == "JellyfishBody" ? DirectorCore.MonsterSpawnDistance.Far : DirectorCore.MonsterSpawnDistance.Standard,
                    preventOverhead = false,
                    minimumStageCompletions = minStages,
                    forbiddenUnlockableDef = null,
                    requiredUnlockableDef = null
                };
                cardList.Add(dc);
            }
            return cardList;
        }
    }
}
