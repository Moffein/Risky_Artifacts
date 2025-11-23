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
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using static Risky_Artifacts.Artifacts.Universe;

namespace Risky_Artifacts.Artifacts
{
    public class Universe
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static DirectorCardCategorySelection MonsterCardSelection;
        public static ItemDef UniverseScriptedEncounterStatItem, UniverseDroneStatItem;
        public static Dictionary<BodyIndex, int> DirectorCosts = new Dictionary<BodyIndex, int>();

        public static SpawnCard.EliteRules fsEliteRules = SpawnCard.EliteRules.ArtifactOnly;
        public static int fsMinStages = 5;
        public static int fsCost = 6000;
        public static bool fsAllowElite = true;

        public static SpawnCard.EliteRules mithrixEliteRules = SpawnCard.EliteRules.ArtifactOnly;
        public static int mithrixMinStages = 5;
        public static int mithrixCost = 4000;
        public static bool mithrixAllowElite = true;

        public static SpawnCard.EliteRules mithrixHurtEliteRules = SpawnCard.EliteRules.ArtifactOnly;
        public static int mithrixHurtMinStages = 5;
        public static int mithrixHurtCost = 12000;
        public static bool mithrixHurtAllowElite = true;

        public static SpawnCard.EliteRules voidlingEliteRules = SpawnCard.EliteRules.ArtifactOnly;
        public static int voidlingMinStages = 5;
        public static int voidlingCost = 8000;
        public static bool voidlingPhase2 = true;
        public static bool voidlingAllowElite = true;

        public static SpawnCard.EliteRules newtEliteRules = SpawnCard.EliteRules.Lunar;
        public static bool newtAllowElite = false;
        public static int newtMinStages = 5;
        public static int newtCost = 12000;

        public static float droneDamageMult = 0.1f;

        public static bool enableOnMoon = false;
        public static bool enableOnVoidLocus = false;
        public static List<SceneDef> forbiddenScenes = new List<SceneDef>();

        public static bool generateConfig;

        public static class InputInfo
        {
            public static string Basic_Monsters = "ClayMongerBody, RunshroomBody, LampBody, ExtractorUnitBody, WorkerUnitBody, TankerBody, ChildBody, LunarExploderBody, BeetleBody, WispBody, LemurianBody, JellyfishBody, HermitCrabBody, VoidBarnacleBody, ImpBody, VultureBody, RoboBallMiniBody, AcidLarvaBody, MinorConstructBody, FlyingVerminBody, VerminBody, MoffeinClayManBody:28, LynxArcherBody:28, LynxHunterBody:28, LynxScoutBody:28, MechanicalSpiderBody:28, ArcherBugBody:28:0:1:Air, SwiftBody:28:0:1:Air";
            public static string Minibosses = "MinePodBody, DefectiveUnitBody, IronHaulerBody, ScorchlingBody, LunarGolemBody, LunarWispBody, GupBody, ClayGrenadierBody, ClayBruiserBody, MiniMushroomBody, BisonBody, BellBody, ParentBody, GolemBody, GreaterWispBody, BeetleGuardBody, NullifierBody, VoidJailerBody, LemurianBruiserBody, MoffeinArchWisp:240, LynxShamanBody:40, SpitterBody:30";
            public static string Champions = "VagrantBody, TitanBody, BeetleQueen2Body, ClayBossBody, MagmaWormBody, ImpBossBody, RoboBallBossBody, GravekeeperBody, MegaConstructBody, VoidMegaCrabBody, GrandparentBody, ScavBody, ElectricWormBody, MoffeinAncientWispBody:1000, MechorillaBody:600, RegigigasBody:1000, LampBossBody:600, LynxTotemBody:600, IfritBody:800, ColossusBody:1150, SolusAmalgamatorBody";
            public static string Special = "TitanGoldBody:6000:5, SuperRoboBallBossBody:6000:5, DireseekerBossBody:6000:5, VultureHunterBody:7700:5";
            public static string LunarScav = "ScavLunar1Body:8000:5, ScavLunar2Body:8000: 5,ScavLunar3Body:8000:5, ScavLunar4Body:8000:5";
            public static string Drone = "Turret1Body, Drone1Body, Drone2Body, MissileDroneBody, FlameDroneBody, EmergencyDroneBody, MegaDroneBody, BackupDroneBody, HaulerDroneBody, BombardmentDroneBody, CopycatDroneBody, JailerDroneBody";
        }

        public static class Categories
        {
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

            public static CategoryInfo CatFalseSonBoss = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.FalseSonBoss
            };

            public static CategoryInfo CatMithrix = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.Mithrix
            };

            public static CategoryInfo CatMithrixHurt = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.MithrixHurt
            };

            public static CategoryInfo CatVoidling = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.Voidling
            };

            public static CategoryInfo CatNewt = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.Newt
            };

            public static CategoryInfo CatDrone = new CategoryInfo()
            {
                weight = 0f,
                category = MonsterCategory.Drone
            };
        }

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
            Basic_Monsters, Minibosses, Champions, Special, LunarScav, Mithrix, MithrixHurt, Voidling, Drone, Newt, FalseSonBoss
        }

        public Universe()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfUniverse";
            artifact.nameToken = "RISKYARTIFACTS_UNIVERSE_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_UNIVERSE_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texUniverseDisabled.png");//todo
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texUniverseEnabled.png");//todo
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            if (!enableOnMoon)
            {
                forbiddenScenes.Add(Addressables.LoadAssetAsync<SceneDef>("RoR2/Base/moon/moon.asset").WaitForCompletion());
                forbiddenScenes.Add(Addressables.LoadAssetAsync<SceneDef>("RoR2/Base/moon2/moon2.asset").WaitForCompletion());
            }

            if (!enableOnVoidLocus)
            {
                forbiddenScenes.Add(Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/voidstage/voidstage.asset").WaitForCompletion());
            }

            BuildItem();

            Universe_Spawncards.Init();
            RoR2Application.onLoad += OnLoad;
            ArtifactHooks();
        }

        private void OnLoad()
        {
            Categories.CatBasicMonsters.cards = ParseSpawnlist(Universe.InputInfo.Basic_Monsters, Categories.CatBasicMonsters.category);
            Categories.CatMinibosses.cards = ParseSpawnlist(Universe.InputInfo.Minibosses, Categories.CatMinibosses.category);
            Categories.CatChampions.cards = ParseSpawnlist(Universe.InputInfo.Champions, Categories.CatChampions.category);
            Categories.CatSpecial.cards = ParseSpawnlist(Universe.InputInfo.Special, Categories.CatSpecial.category);
            Categories.CatLunarScav.cards = ParseSpawnlist(Universe.InputInfo.LunarScav, Categories.CatLunarScav.category);
            Categories.CatDrone.cards = ParseSpawnlist(Universe.InputInfo.Drone, Categories.CatDrone.category);

            //Special cards are separate so they don't get overwritten
            BuildCatFalseSonBoss();
            BuildCatMithrix();
            BuildCatMithrixHurt();
            BuildCatVoidling();
            BuildCatNewt();

            MonsterCardSelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            MonsterCardSelection.name = "UniverseMonsterCardSelection";
            (MonsterCardSelection as DirectorCardCategorySelection).name = MonsterCardSelection.name;

            if (Categories.CatBasicMonsters.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Basic Monsters", Categories.CatBasicMonsters.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatBasicMonsters.cards.ToArray();
            }

            if (Categories.CatMinibosses.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Minibosses", Categories.CatMinibosses.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatMinibosses.cards.ToArray();
            }

            if (Categories.CatChampions.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Champions", Categories.CatChampions.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatChampions.cards.ToArray();
            }

            if (Categories.CatSpecial.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Special", Categories.CatSpecial.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatSpecial.cards.ToArray();
            }

            if (Categories.CatMithrix.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Mithrix", Categories.CatMithrix.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatMithrix.cards.ToArray();
            }

            if (Categories.CatMithrixHurt.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Mithrix Hurt", Categories.CatMithrixHurt.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatMithrixHurt.cards.ToArray();
            }

            if (Categories.CatVoidling.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Voidling", Categories.CatVoidling.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatVoidling.cards.ToArray();
            }

            if (Categories.CatNewt.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Newt", Categories.CatNewt.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatNewt.cards.ToArray();
            }

            if (Categories.CatLunarScav.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Newt", Categories.CatNewt.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatLunarScav.cards.ToArray();
            }

            if (Categories.CatDrone.weight > 0f)
            {
                MonsterCardSelection.AddCategory("Drone", Categories.CatDrone.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatDrone.cards.ToArray();
            }

            if (Categories.CatFalseSonBoss.weight > 0f)
            {
                MonsterCardSelection.AddCategory("FalseSonBoss", Categories.CatFalseSonBoss.weight);
                MonsterCardSelection.categories[MonsterCardSelection.categories.Length - 1].cards = Categories.CatFalseSonBoss.cards.ToArray();
            }
        }

        private void BuildCatFalseSonBoss()
        {
            CharacterSpawnCard fsCardOrig = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC2/FalseSonBoss/cscFalseSonBoss.asset").WaitForCompletion();

            CharacterSpawnCard fsCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            fsCard.prefab = fsCardOrig.prefab;
            fsCard.directorCreditCost = fsCost;
            fsCard.itemsToGrant = new ItemCountPair[]
            {
                new ItemCountPair()
                {
                    itemDef = RoR2Content.Items.TeleportWhenOob,
                    count = 1
                },
                new ItemCountPair()
                {
                    itemDef = UniverseScriptedEncounterStatItem,
                    count = 1
                },
                new ItemCountPair()
                {
                    itemDef = RoR2Content.Items.AdaptiveArmor,
                    count = 1
                }
            };
            fsCard.eliteRules = mithrixEliteRules;
            fsCard.noElites = !mithrixAllowElite;
            fsCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            fsCard.occupyPosition = false;
            fsCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            fsCard.loadout = fsCardOrig.loadout;
            fsCard.sendOverNetwork = true;
            fsCard.hullSize = HullClassification.Golem;
            fsCard.name = "cscUniverseFalseSonBossUnique";
            (fsCard as ScriptableObject).name = fsCard.name;

            DirectorCard dc = new DirectorCard()
            {
                selectionWeight = 1,
                spawnCard = fsCard,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                preventOverhead = false,
                minimumStageCompletions = fsMinStages,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            Categories.CatFalseSonBoss.cards = new List<DirectorCard>() { dc };
        }
        private void BuildCatMithrix()
        {
            CharacterSpawnCard itBrotherCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/ITAssets/cscBrotherIT.asset").WaitForCompletion();

            CharacterSpawnCard brotherCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            brotherCard.prefab = itBrotherCard.prefab;
            brotherCard.directorCreditCost = mithrixCost;

            brotherCard.itemsToGrant = new ItemCountPair[]
            {
                new ItemCountPair()
                {
                    itemDef = RoR2Content.Items.TeleportWhenOob,
                    count = 1
                },
                new ItemCountPair()
                {
                    itemDef = UniverseScriptedEncounterStatItem,
                    count = 1
                },
                new ItemCountPair()
                {
                    itemDef = RoR2Content.Items.AdaptiveArmor,
                    count = 1
                }
            };
            brotherCard.eliteRules = mithrixEliteRules;
            brotherCard.noElites = !mithrixAllowElite;
            brotherCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            brotherCard.occupyPosition = false;
            brotherCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            brotherCard.loadout = itBrotherCard.loadout;
            brotherCard.sendOverNetwork = true;
            brotherCard.hullSize = HullClassification.Golem;
            brotherCard.name = "cscUniverseBrotherUnique";
            (brotherCard as ScriptableObject).name = brotherCard.name;

            DirectorCard dc = new DirectorCard()
            {
                selectionWeight = 1,
                spawnCard = brotherCard,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                preventOverhead = false,
                minimumStageCompletions = mithrixMinStages,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            Categories.CatMithrix.cards = new List<DirectorCard>() { dc };
        }

        public static bool BodyIsFlying(GameObject bodyObject)
        {
            CharacterBody body = bodyObject ? bodyObject.GetComponent<CharacterBody>() : null;
            if (!body) return false;
            return BodyIsFlying(body);
        }

        //This will miss things that don't use FlyState to fly
        public static bool BodyIsFlying(CharacterBody body)
        {
            if (body)
            {
                NetworkStateMachine nsm = body.GetComponent<NetworkStateMachine>();
                if (nsm && nsm.stateMachines != null)
                {
                    foreach (var sm in nsm.stateMachines)
                    {
                        if (sm.initialStateType.stateType.IsAssignableFrom(typeof(EntityStates.FlyState)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void BuildCatMithrixHurt()
        {
            CharacterSpawnCard itBrotherCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/cscBrotherIT.asset").WaitForCompletion();

            CharacterSpawnCard brotherHurtCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            brotherHurtCard.prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherHurtMaster.prefab").WaitForCompletion();
            brotherHurtCard.directorCreditCost = mithrixHurtCost;
            brotherHurtCard.itemsToGrant = new ItemCountPair[]
            {
                new ItemCountPair()
                {
                    itemDef = UniverseScriptedEncounterStatItem,
                    count = 1
                }
            };
            brotherHurtCard.eliteRules = mithrixHurtEliteRules;
            brotherHurtCard.noElites = !mithrixHurtAllowElite;
            brotherHurtCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            brotherHurtCard.occupyPosition = false;
            brotherHurtCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            brotherHurtCard.loadout = new SerializableLoadout();
            brotherHurtCard.sendOverNetwork = true;
            brotherHurtCard.hullSize = HullClassification.Golem;
            brotherHurtCard.name = "cscUniverseBrotherHurtUnique";
            (brotherHurtCard as ScriptableObject).name = brotherHurtCard.name;

            DirectorCard dc = new DirectorCard()
            {
                selectionWeight = 1,
                spawnCard = brotherHurtCard,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                preventOverhead = false,
                minimumStageCompletions = mithrixHurtMinStages,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            Categories.CatMithrixHurt.cards = new List<DirectorCard>() { dc };
        }

        private void BuildCatVoidling()
        {
            string prefabPath = voidlingPhase2 ? "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset" : "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset";
            CharacterSpawnCard origCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(prefabPath).WaitForCompletion();

            CharacterSpawnCard voidlingCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            voidlingCard.prefab = origCard.prefab;
            voidlingCard.directorCreditCost = voidlingCost;
            voidlingCard.itemsToGrant = new ItemCountPair[]
            {
                new ItemCountPair()
                {
                    itemDef = UniverseScriptedEncounterStatItem,
                    count = 1
                },
                new ItemCountPair()
                {
                    itemDef = RoR2Content.Items.AdaptiveArmor,
                    count = 1
                }
            };
            voidlingCard.eliteRules = voidlingEliteRules;
            voidlingCard.noElites = !voidlingAllowElite;
            voidlingCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Air;
            voidlingCard.occupyPosition = false;
            voidlingCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            voidlingCard.hullSize = origCard.hullSize;
            voidlingCard.loadout = origCard.loadout;
            voidlingCard.sendOverNetwork = true;
            voidlingCard.name = "cscUniverseVoidlingUnique";
            (voidlingCard as ScriptableObject).name = voidlingCard.name;

            DirectorCard dc = new DirectorCard()
            {
                selectionWeight = 1,
                spawnCard = voidlingCard,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
                preventOverhead = false,
                minimumStageCompletions = mithrixHurtMinStages,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            Categories.CatVoidling.cards = new List<DirectorCard>() { dc };
        }

        private void BuildCatNewt()
        {
            CharacterSpawnCard newtCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            newtCard.prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Shopkeeper/ShopkeeperMaster.prefab").WaitForCompletion();
            newtCard.directorCreditCost = newtCost;
            newtCard.eliteRules = newtEliteRules;
            newtCard.noElites = !newtAllowElite;
            newtCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            newtCard.occupyPosition = false;
            newtCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            newtCard.hullSize = HullClassification.Golem;
            newtCard.loadout = new SerializableLoadout();
            newtCard.sendOverNetwork = true;
            newtCard.name = "cscUniverseNewtUnique";
            (newtCard as ScriptableObject).name = newtCard.name;

            BodyIndex newtIndex = BodyCatalog.FindBodyIndex("ShopkeeperBody");
            if (DirectorCosts.ContainsKey(newtIndex)) DirectorCosts.Remove(newtIndex);
            DirectorCosts.Add(newtIndex, newtCost);

            DirectorCard dc = new DirectorCard()
            {
                selectionWeight = 1,
                spawnCard = newtCard,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                preventOverhead = false,
                minimumStageCompletions =newtMinStages,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            Categories.CatNewt.cards = new List<DirectorCard>() { dc };
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
                        if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Universe.artifact) && !forbiddenScenes.Contains(SceneCatalog.GetSceneDefForCurrentScene()))
                        {
                            //Make a copy so that the original list doesn't get modified.
                            DirectorCardCategorySelection newDCCS = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                            newDCCS.CopyFrom(Universe.MonsterCardSelection);
                            return newDCCS;
                        }
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


            UniverseDroneStatItem = ScriptableObject.CreateInstance<ItemDef>();
            UniverseDroneStatItem.name = "RiskyArtifactsUniverseDroneStatItem";
            UniverseDroneStatItem.deprecatedTier = ItemTier.NoTier;
            UniverseDroneStatItem.nameToken = "RISKYARTIFACTS_UNIVERSEDRONESTATITEM_NAME";
            UniverseDroneStatItem.pickupToken = "RISKYARTIFACTS_UNIVERSEDRONESTATITEM_PICKUP";
            UniverseDroneStatItem.descriptionToken = "RISKYARTIFACTS_UNIVERSEDRONESTATITEM_DESC";
            UniverseDroneStatItem.tags = new[]
            {
                ItemTag.WorldUnique,
                ItemTag.BrotherBlacklist,
                ItemTag.CannotSteal
            };

            ItemAPI.Add(new CustomItem(UniverseScriptedEncounterStatItem, new ItemDisplayRule[0]));
            ItemAPI.Add(new CustomItem(UniverseDroneStatItem, new ItemDisplayRule[0]));

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody body)
        {
            if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Universe.artifact) && body.teamComponent && body.teamComponent.teamIndex != TeamIndex.Player)
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene != null && currentScene.name == "bazaar") return;

                DeathRewards dr = body.GetComponent<DeathRewards>();
                if (!dr)
                {
                    dr = body.gameObject.AddComponent<DeathRewards>();
                    dr.logUnlockableDef = null;

                    int cost = 0;
                    Universe.DirectorCosts.TryGetValue(body.bodyIndex, out cost);

                    dr.spawnValue = cost;
                    if (body.isElite) cost = Mathf.RoundToInt(cost * 1.5f); //Just a lazy way of scaling cost.

                    float diffMult = Run.instance ? Run.instance.difficultyCoefficient : 1f;
                    float calcXP = cost * diffMult;
                    float calcGold = 2f * cost * diffMult;  //Wiki says that this should be x2

                    CombatDirector firstActiveCombatDirector = CombatDirector.instancesList.FirstOrDefault(director => director.isActiveAndEnabled);
                    if (firstActiveCombatDirector)
                    {
                        calcXP *= firstActiveCombatDirector.expRewardCoefficient;
                        //calcGold *= firstActiveCombatDirector.goldRewardCoefficient;
                    }
                    calcGold *= 0.2f;   //Can't be arsed to figure out how to properly reduce the gold reward.

                    dr.expReward = (uint)Mathf.Max(1, Mathf.FloorToInt(calcXP));
                    dr.goldReward = (uint)Mathf.Max(1, Mathf.FloorToInt(calcGold));

                    dr.bossPickup = new SerializablePickupIndex { pickupName = "" };
                }
            }
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory && self.inventory.GetItemCount(Universe.UniverseDroneStatItem) > 0)
            {
                self.damage *= Universe.droneDamageMult;
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.inventory)
            {
                if (sender.inventory.GetItemCount(Universe.UniverseScriptedEncounterStatItem) > 0)
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
                int directorCardWeight = 1;
                string graphTypeString = string.Empty;

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
                            case 3:
                                if (int.TryParse(current[3], out int parsedWeight))
                                {
                                    directorCardWeight = parsedWeight;
                                }
                                break;
                            case 4:
                                graphTypeString = current[4];
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

                //Set body graphType
                RoR2.Navigation.MapNodeGroup.GraphType nodeGraphType;
                graphTypeString = graphTypeString.ToLower();
                switch(graphTypeString)
                {
                    case "air":
                        nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Air;
                        break;
                    case "ground":
                        nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                        break;
                    case "rail":
                        nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Rail;
                        break;
                    default:
                        nodeGraphType = BodyIsFlying(body) ? RoR2.Navigation.MapNodeGroup.GraphType.Air : RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                        break;
                }

                //Build new card
                CharacterSpawnCard newCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                newCard.prefab = masterPrefab;
                newCard.directorCreditCost = cost;

                if (DirectorCosts.ContainsKey(index)) DirectorCosts.Remove(index);
                DirectorCosts.Add(index, cost);

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
                    newCard.itemsToGrant = origSpawnCard.itemsToGrant;
                }
                else
                {
                    newCard.eliteRules = SpawnCard.EliteRules.Default;
                    newCard.forbiddenAsBoss = false;
                    newCard.hullSize = body.hullClassification;
                    newCard.noElites = false;
                    newCard.nodeGraphType = nodeGraphType;
                    newCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
                    newCard.occupyPosition = false;
                    newCard.loadout = new SerializableLoadout();
                    newCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
                    newCard.itemsToGrant = new ItemCountPair[0];
                }
                newCard.sendOverNetwork = true;
                newCard.name = "cscUniverse" + name;
                (newCard as ScriptableObject).name = newCard.name;

                //Estimate Cost
                if (newCard.directorCreditCost < 0)
                {
                    float hp = (body.baseMaxHealth + body.maxBarrier) * (100f + body.armor) / 100f;
                    bool isFlying = BodyIsFlying(body);
                    switch (monsterCategory)
                    {
                        case MonsterCategory.LunarScav:
                            newCard.directorCreditCost = 6000;
                            break;
                        case MonsterCategory.Special:
                            newCard.directorCreditCost = 4000;
                            break;
                        case MonsterCategory.Champions:
                            newCard.directorCreditCost = Mathf.RoundToInt(hp / 3.5f);
                            break;
                        case MonsterCategory.Minibosses:
                            newCard.directorCreditCost = Mathf.RoundToInt(hp / (isFlying ? 3.5f : 7f));
                            break;
                        case MonsterCategory.Basic_Monsters:
                        default:
                            newCard.directorCreditCost = Mathf.RoundToInt(hp / (isFlying ? 3.5f : 7.5f));
                            break;
                    }
                }

                if (newCard.itemsToGrant == null) newCard.itemsToGrant = new ItemCountPair[0];
                List<ItemCountPair> itemList = newCard.itemsToGrant.ToList();
                if (monsterCategory == MonsterCategory.Special || monsterCategory == MonsterCategory.LunarScav)
                {
                    itemList.Add(new ItemCountPair()
                    {
                        itemDef = Universe.UniverseScriptedEncounterStatItem,
                        count = 1
                    });
                }
                else if (monsterCategory == MonsterCategory.Drone)
                {
                    itemList.Add(new ItemCountPair()
                    {
                        itemDef = Universe.UniverseDroneStatItem,
                        count = 1
                    });
                }
                newCard.itemsToGrant = itemList.ToArray();

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

                //Jellyfish thing is jank, but it's the only monster that actually uses this spawndistance.
                DirectorCard dc = new DirectorCard()
                {
                    selectionWeight = directorCardWeight,
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
