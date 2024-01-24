using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static Dictionary<BodyIndex, SpawnInfo> StatOverrideBodies = new Dictionary<BodyIndex, SpawnInfo>();
        public static Dictionary<BodyIndex, int> DirectorCosts = new Dictionary<BodyIndex, int>();

        private List<SpawnInfo> SpawnInfoList = new List<SpawnInfo>();
        public static string spawnInfoInput;
        public static bool allSurvivors = false;
        public static bool nerfEngiTurrets = true;
        public static bool nerfPercentHeal = true;
        public static bool useOverlay = true;
        public static bool disableRegen = true;
        public static bool allowElite = true;
        public static bool allowCruelty = true;

        public static bool survivorsOnly = false;
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static ItemDef HuntedStatItem;

        public static float categoryWeight = 1f;
        public static float healthMult = 10f;
        public static float damageMult = 0.2f;
        public static int directorCost = 200;

        public Hunted()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfHunted";
            artifact.nameToken = "RISKYARTIFACTS_HUNTED_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_HUNTED_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedEnabled.png");
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
                float hpOverride = Hunted.healthMult;
                float damageOverride = Hunted.damageMult;

                bool shouldOverride = false;
                if (current.Length > 1)
                {
                    for (int i = 1; i < current.Length; i++)
                    {
                        switch(i)
                        {
                            case 1:
                                if (int.TryParse(current[1], out int parsedCost))
                                {
                                    cost = parsedCost;
                                }    
                                break;
                            case 2:
                                if (float.TryParse(current[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedHP))
                                {
                                    shouldOverride = true;
                                    hpOverride = parsedHP;
                                }
                                break;
                            case 3:
                                if (float.TryParse(current[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedDamage))
                                {
                                    shouldOverride = true;
                                    damageOverride = parsedDamage;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                SpawnInfo spawnInfo = new SpawnInfo()
                {
                    isOverride = shouldOverride,
                    bodyName = name,
                    directorCost = cost,
                    hpMultOverride = hpOverride,
                    damageMultOverride = damageOverride
                };
                SpawnInfoList.Add(spawnInfo);
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
                Debug.Log("RiskyArtifacts: Hunted: Creating card for " + info.bodyIndex + " - " + info.bodyName);
                GameObject master = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(info.bodyIndex));
                if (!master)
                {
                    Debug.LogError("RiskyArtifacts: Hunted:  Master could not be found for survivor " + info.bodyIndex + " - " + info.bodyName);
                    continue;
                }

                if (info.isOverride)
                {
                    StatOverrideBodies.Add(info.bodyIndex, info);
                }

                CharacterSpawnCard csc = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                csc.prefab = master;
                csc.directorCreditCost = info.directorCost;
                csc.eliteRules = SpawnCard.EliteRules.Default;
                csc.forbiddenAsBoss = false;
                csc.hullSize = HullClassification.Human;
                csc.noElites = Hunted.allowElite;
                csc.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                csc.itemsToGrant = new ItemCountPair[] {
                    new ItemCountPair() { itemDef = Hunted.HuntedStatItem, count = 1 },
                    new ItemCountPair() { itemDef = RoR2Content.Items.TeleportWhenOob, count = 1 },
                };
                csc.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
                csc.occupyPosition = false;
                csc.loadout = new SerializableLoadout();
                csc.sendOverNetwork = true;
                csc.requiredFlags = RoR2.Navigation.NodeFlags.None;
                csc.name = "cscHunted" + info.bodyIndex + info.bodyName;
                (csc as ScriptableObject).name = csc.name;
                SpawnCards.Add(csc);

                if (DirectorCosts.ContainsKey(info.bodyIndex))
                {
                    DirectorCosts.Remove(info.bodyIndex);
                }
                DirectorCosts.Add(info.bodyIndex, info.directorCost);

                DirectorCard dc = new DirectorCard()
                {
                    selectionWeight = 1,
                    spawnCard = csc,
                    spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                    preventOverhead = false,
                    minimumStageCompletions = 0,
                    forbiddenUnlockableDef = null,
                    requiredUnlockableDef = null
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
            On.RoR2.CharacterBody.RecalculateStats += ReduceHuntedDamage;
            RecalculateStatsAPI.GetStatCoefficients += SetHuntedHealth;
            if (Hunted.nerfPercentHeal)On.RoR2.HealthComponent.HealFraction += ReduceHuntedPercentHeal;

            CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;
            IL.RoR2.Util.GetBestBodyName += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                         x => x.MatchLdsfld(typeof(RoR2Content.Items), "InvadingDoppelganger")
                        );
                c.Index += 2;
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Func<int, CharacterBody, int>>((vengeanceCount, body) =>
                {
                    int toReturn = vengeanceCount;
                    if (body && body.inventory)
                    {
                        toReturn += body.inventory.GetItemCount(HuntedStatItem);
                    }
                    return toReturn;
                });
            };
        }

        private void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody body)
        {
            body.AddItemBehavior<HuntedItemBehavior>(body.inventory.GetItemCount(Hunted.HuntedStatItem));
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

        private void SetHuntedHealth(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.inventory && sender.inventory.GetItemCount(Hunted.HuntedStatItem) > 0 )
            {
                if (!(Hunted.nerfEngiTurrets && Hunted.TurretNerfList.Contains(sender.bodyIndex)))
                {
                    float healthMult = Hunted.healthMult;
                    if (StatOverrideBodies.TryGetValue(sender.bodyIndex, out SpawnInfo info)) healthMult = info.hpMultOverride;
                    args.healthMultAdd += healthMult - 1f;
                }

                if (disableRegen)
                {
                    args.baseRegenAdd -= sender.baseRegen + (sender.level - 1f) * sender.levelRegen;
                }
            }
        }

        private float ReduceHuntedPercentHeal(On.RoR2.HealthComponent.orig_HealFraction orig, HealthComponent self, float fraction, ProcChainMask procChainMask)
        {
            if (self.body.inventory && self.body.inventory.GetItemCount(Hunted.HuntedStatItem) > 0)
            {
                float healthMult = Hunted.healthMult;
                if (StatOverrideBodies.TryGetValue(self.body.bodyIndex, out SpawnInfo info)) healthMult = info.hpMultOverride;
                fraction /= healthMult;
            }
            return orig(self, fraction, procChainMask);
        }

        private void ReduceHuntedDamage(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory && self.inventory.GetItemCount(Hunted.HuntedStatItem) > 0)
            {
                float mult = Hunted.damageMult;
                if (StatOverrideBodies.TryGetValue(self.bodyIndex, out SpawnInfo info)) mult = info.damageMultOverride;
                self.damage *= mult;
            }
        }

        private void ArtifactHooks()
        {
            On.RoR2.ClassicStageInfo.OnArtifactEnabled += ClassicStageInfo_OnArtifactEnabled;
            On.RoR2.ClassicStageInfo.OnArtifactDisabled += ClassicStageInfo_OnArtifactDisabled;

            //Add Hunted Survivors to enemy spawning
            IL.RoR2.ClassicStageInfo.RebuildCards += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchStfld(typeof(ClassicStageInfo), "modifiableMonsterCategories")))
                {
                    c.EmitDelegate<Func<DirectorCardCategorySelection, DirectorCardCategorySelection>>(dccs =>
                    {
                        if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Hunted.artifact)) CreateHuntedCategory(dccs);
                        return dccs;
                    });
                }
                else
                {
                    Debug.Log("RiskyArtifacts: Hunted ClassicStageInfo.RebuildCards IL Hook failed.");
                }
            };

            On.RoR2.ClassicStageInfo.HandleMixEnemyArtifact += ClassicStageInfo_HandleMixEnemyArtifact;
        }

        private void ClassicStageInfo_OnArtifactEnabled(On.RoR2.ClassicStageInfo.orig_OnArtifactEnabled orig, ClassicStageInfo self, RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            orig(self, runArtifactManager, artifactDef);
            if (artifactDef == Hunted.artifact) self.RebuildCards();
        }

        private void ClassicStageInfo_OnArtifactDisabled(On.RoR2.ClassicStageInfo.orig_OnArtifactDisabled orig, ClassicStageInfo self, RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            orig(self, runArtifactManager, artifactDef);
            if (artifactDef == Hunted.artifact) self.RebuildCards();
        }

        private void ClassicStageInfo_HandleMixEnemyArtifact(On.RoR2.ClassicStageInfo.orig_HandleMixEnemyArtifact orig, DirectorCardCategorySelection monsterCategories, Xoroshiro128Plus rng)
        {
            orig(monsterCategories, rng);
            if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Hunted.artifact)) CreateHuntedCategory(monsterCategories);
        }

        private static void CreateHuntedCategory(DirectorCardCategorySelection selection)
        {
            if (!selection) return;
            if (survivorsOnly)
            {
                selection.categories = new DirectorCardCategorySelection.Category[0];
            }
            selection.AddCategory("RiskyArtifactsHunted", Hunted.categoryWeight);
            selection.categories[selection.categories.Length - 1].cards = DirectorCards.ToArray();
        }

        //Used to construct spawncards when reading from config
        public class SpawnInfo
        {
            public string bodyName;
            public BodyIndex bodyIndex = BodyIndex.None;
            public int directorCost;

            //Only used if the config specifies this body needs an override
            public bool isOverride = false;
            public float hpMultOverride;
            public float damageMultOverride;
        }
    }

    public class HuntedItemBehavior : CharacterBody.ItemBehavior
    {
        private bool origImmuneToExecute = true;

        private void Start()
        {
            origImmuneToExecute = body && body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes);
            if (origImmuneToExecute) body.bodyFlags &= ~CharacterBody.BodyFlags.ImmuneToExecutes;

            DeathRewards dr = body.GetComponent<DeathRewards>();
            if (!dr && body.teamComponent && body.teamComponent.teamIndex != TeamIndex.Player)
            {
                dr = body.gameObject.AddComponent<DeathRewards>();
                dr.logUnlockableDef = null;

                int cost = 0;
                Hunted.DirectorCosts.TryGetValue(body.bodyIndex, out cost);

                dr.spawnValue = cost;
                if (body.isElite) cost = Mathf.RoundToInt(cost * 1.5f); //Just a lazy way of scaling cost.

                float diffMult = Run.instance ? Run.instance.difficultyCoefficient : 1f;
                float calcXP = cost * diffMult;
                float calcGold = cost * diffMult;

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

        private void OnDisable()
        {
            if (origImmuneToExecute && body) body.bodyFlags |= CharacterBody.BodyFlags.ImmuneToExecutes;
        }
    }
}
