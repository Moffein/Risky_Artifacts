using UnityEngine;
using RoR2;
using R2API;
using System.Collections.Generic;
using MonoMod.Cil;
using System;
using UnityEngine.Networking;
using Mono.Cecil.Cil;

namespace Risky_Artifacts.Artifacts
{
    public class BrotherInvasion
    {
        public static bool enabled = true;
        public static ItemDef BrotherInvasionBonusItem;
        public static ArtifactDef artifact;
        //public static GameObject networkBrotherSpawner;
        public static bool bossLunarTeam = true;
        public static bool ignoreHonor = false;

        public BrotherInvasion()
        {
            if (!enabled) return;

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfBrotherInvasion";
            artifact.nameToken = "RISKYARTIFACTS_BROTHERINVASION_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_BROTHERINVASION_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texPhantomDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texPhantomEnabled.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            /*networkBrotherSpawner = new GameObject();
            networkBrotherSpawner.AddComponent<NetworkIdentity>();
            networkBrotherSpawner.AddComponent<Risky_Artifacts.Artifacts.MonoBehaviours.BrotherInvasionController>();
            PrefabAPI.RegisterNetworkPrefab(networkBrotherSpawner);
            ContentAddition.AddNetworkedObject(networkBrotherSpawner);*/

            RoR2.Stage.onServerStageBegin += CreateBrotherSpawner;

            InitBrotherItem();
        }

        private void InitBrotherItem()
        {
            BrotherInvasionBonusItem = ScriptableObject.CreateInstance<ItemDef>();
            BrotherInvasionBonusItem.name = "RiskyArtifactsBrotherInvasionBonus";
            BrotherInvasionBonusItem.deprecatedTier = ItemTier.NoTier;
            BrotherInvasionBonusItem.nameToken = "RISKYARTIFACTS_BROTHERINVASIONBONUSITEM_NAME";
            BrotherInvasionBonusItem.pickupToken = "RISKYARTIFACTS_BROTHERINVASIONBONUSITEM_PICKUP";
            BrotherInvasionBonusItem.descriptionToken = "RISKYARTIFACTS_BROTHERINVASIONBONUSITEM_DESC";
            BrotherInvasionBonusItem.tags = new[]
            {
                ItemTag.WorldUnique,
                ItemTag.BrotherBlacklist,
                ItemTag.CannotSteal
            };
            ItemDisplayRule[] idr = new ItemDisplayRule[0];
            //ContentAddition.AddItemDef(BrotherInvasionBonusItem);
            ItemAPI.Add(new CustomItem(BrotherInvasionBonusItem, idr));

            IL.RoR2.CharacterModel.UpdateOverlays += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                     x => x.MatchCall(typeof(CharacterModel), "get_isGhost")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, CharacterModel, bool>>((isGhost, self) =>
                {
                    bool flag = false;
                    if (self.body && self.body.inventory)
                    {
                        flag = self.body.inventory.GetItemCount(BrotherInvasionBonusItem) > 0;
                    }
                    return isGhost || flag;
                });
            };

            IL.RoR2.CharacterModel.UpdateRendererMaterials += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                     x => x.MatchCall(typeof(CharacterModel), "get_isGhost")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, CharacterModel, bool>>((isGhost, self) =>
                {
                    bool flag = false;
                    if (self.body && self.body.inventory)
                    {
                        flag = self.body.inventory.GetItemCount(BrotherInvasionBonusItem) > 0;
                    }
                    return isGhost || flag;
                });
            };

            On.RoR2.CharacterBody.GetSubtitle += (orig, self) =>
            {
                if (self.inventory && self.inventory.GetItemCount(BrotherInvasionBonusItem) > 0)
                {
                    return Language.GetString("RISKYARTIFACTS_BROTHERINVASION_SUBTITLENAMETOKEN");
                }
                return orig(self);
            };

            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (NetworkServer.active)
                {
                    if (!damageInfo.attacker && !damageInfo.inflictor
                        && damageInfo.damageColorIndex == DamageColorIndex.Void
                        && damageInfo.damageType == (DamageType.BypassArmor | DamageType.BypassBlock))
                    {
                        if (self.body.inventory && self.body.inventory.GetItemCount(BrotherInvasion.BrotherInvasionBonusItem) > 0)
                        {
                            damageInfo.damage = 0f;
                            damageInfo.rejected = true;
                        }
                    }
                }
                orig(self, damageInfo);
            };
        }

        private void CreateBrotherSpawner(Stage obj)
        {
            if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(BrotherInvasion.artifact))
            {
                SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
                if (currentScene && !currentScene.isFinalStage && currentScene.sceneType == SceneType.Stage)
                {
                    if (obj)
                    {
                        obj.gameObject.AddComponent<MonoBehaviours.BrotherInvasionController>();
                    }
                    /*GameObject go = GameObject.Instantiate(networkBrotherSpawner);
                    NetworkServer.Spawn(go);*/
                }
            }
        }
    }
}
