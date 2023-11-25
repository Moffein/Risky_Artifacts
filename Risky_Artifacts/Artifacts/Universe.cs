using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Timeline;

namespace Risky_Artifacts.Artifacts
{
    public class Universe
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static DirectorCardCategorySelection MonsterCardSelection;

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

        public class CategoryInfo
        {
            public MonsterCategory category;
            public float weight;
            public List<DirectorCard> cards;
        }

        public static class DirectorCards
        {
            public static List<DirectorCard> Basic_Monsters = new List<DirectorCard>();
            public static List<DirectorCard> Minibosses = new List<DirectorCard>();
            public static List<DirectorCard> Champions = new List<DirectorCard>();
            public static List<DirectorCard> Special = new List<DirectorCard>();
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
            Basic_Monsters, Minibosses, Champions, Special, LunarScav, Mithrix, Voidling, Drone
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

            //Just copied from RiskyMod
            Universe_Spawncards.Init();
            //Universe_DirectorCards.Init();

            RoR2Application.onLoad += OnLoad;
            ArtifactHooks();
        }

        private void OnLoad()
        {
            CatBasicMonsters.cards = ParseSpawnlist(Universe.InputInfo.Basic_Monsters, MonsterCategory.Basic_Monsters);
            CatMinibosses.cards = ParseSpawnlist(Universe.InputInfo.Minibosses, MonsterCategory.Minibosses);
            CatChampions.cards = ParseSpawnlist(Universe.InputInfo.Champions, MonsterCategory.Champions);
            CatSpecial.cards = ParseSpawnlist(Universe.InputInfo.Special, MonsterCategory.Special);

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

                Universe_Spawncards.CardDict.TryGetValue(name, out CharacterSpawnCard spawnCard);
                int cost = spawnCard ? spawnCard.directorCreditCost : -1;
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
                                    if (spawnCard) spawnCard.directorCreditCost = parsedCost;
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

                if (!spawnCard)
                {
                    Debug.LogWarning("RiskyArtifacts: Universe: Could not find spawncard for " + name + ".");

                    BodyIndex index = BodyCatalog.FindBodyIndex(name);
                    if (index != BodyIndex.None)
                    {
                        GameObject masterPrefab = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(index));
                        if (!masterPrefab)
                        {
                            Debug.LogError("RiskyArtifacts: Universe: Could not create spawncard for " + name + ", AI Master not found.");
                            continue;
                        }

                        spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                        spawnCard.prefab = masterPrefab;
                        spawnCard.directorCreditCost = cost;
                        spawnCard.eliteRules = SpawnCard.EliteRules.Default;
                        spawnCard.forbiddenAsBoss = false;
                        spawnCard.hullSize = HullClassification.Human;
                        spawnCard.noElites = false;
                        spawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                        spawnCard.itemsToGrant = new ItemCountPair[] {};
                        spawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
                        spawnCard.occupyPosition = false;
                        spawnCard.loadout = new SerializableLoadout();
                        spawnCard.sendOverNetwork = true;
                        spawnCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
                        spawnCard.name = "cscUniverse" + index + name;
                        (spawnCard as ScriptableObject).name = spawnCard.name;

                        //Estimate Cost
                        if (spawnCard.directorCreditCost < 0)
                        {
                            GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(index);
                            CharacterBody body = bodyPrefab ? bodyPrefab.GetComponent<CharacterBody>() : null;
                            if (!body)
                            {
                                Debug.LogError("RiskyArtifacts: Universe: Could not create spawncard for " + name + ", Body not found.");
                                UnityEngine.Object.Destroy(spawnCard);
                                continue;
                            }

                            switch (monsterCategory)
                            {
                                case MonsterCategory.Special:
                                    spawnCard.directorCreditCost = 4000;
                                    spawnCard.itemsToGrant = new ItemCountPair[] {
                                        new ItemCountPair()
                                        {
                                            itemDef = RoR2Content.Items.BoostDamage,
                                            count = 10
                                        },
                                        new ItemCountPair()
                                        {
                                            itemDef = RoR2Content.Items.BoostHp,
                                            count = 40
                                        },
                                    };
                                    break;
                                case MonsterCategory.Champions:
                                    spawnCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / 3.5f);
                                    break;
                                case MonsterCategory.Minibosses:
                                    spawnCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / (body.isFlying ? 3.5f : 7f));
                                    break;
                                case MonsterCategory.Basic_Monsters:
                                default:
                                    spawnCard.directorCreditCost = Mathf.RoundToInt(body.baseMaxHealth / (body.isFlying ? 3.5f : 7.5f));
                                    break;
                            }
                        }

                        bool cardExists = Universe_Spawncards.CardDict.ContainsKey(name);
                        if (cardExists)
                        {
                            Debug.LogWarning("RiskyArtifacts: Universe: Spawncard for " + name + " already exists. Overwriting.");
                            Universe_Spawncards.CardDict[name] = spawnCard;
                        }
                        else
                        {
                            Universe_Spawncards.CardDict.Add(name, spawnCard);
                        }
                        Debug.Log("RiskyArtifacts: Universe: Created spawncard for " + name + " with cost " + spawnCard.directorCreditCost + ".");
                    }
                }

                //Jellyfish thing is jank, but it's the only monster that actually uses this.
                DirectorCard dc = new DirectorCard()
                {
                    selectionWeight = 1,
                    spawnCard = spawnCard,
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
