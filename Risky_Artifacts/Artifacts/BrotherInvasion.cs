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
        public static GameObject networkBrotherSpawner;

        public BrotherInvasion()
        {
            if (!enabled) return;

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfBrotherInvasion";
            artifact.nameToken = "RISKYARTIFACTS_BROTHERINVASION_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_BROTHERINVASION_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texOriginEnabledClean.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            networkBrotherSpawner = new GameObject();
            networkBrotherSpawner.AddComponent<NetworkIdentity>();
            networkBrotherSpawner.AddComponent<Risky_Artifacts.Artifacts.MonoBehaviours.BrotherInvasionController>();
            PrefabAPI.RegisterNetworkPrefab(networkBrotherSpawner);
            ContentAddition.AddNetworkedObject(networkBrotherSpawner);

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

            RoR2.Stage.onServerStageBegin += CreateBrotherSpawner;

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
                        toReturn += self.body.inventory.GetItemCount(BrotherInvasionBonusItem);
                    }
                    return toReturn;
                });
            };
        }

        private void CreateBrotherSpawner(Stage obj)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(BrotherInvasion.artifact))
            {
                SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
                if (currentScene && !currentScene.isFinalStage && currentScene.sceneType == SceneType.Stage)
                {
                    GameObject go = GameObject.Instantiate(networkBrotherSpawner);
                    NetworkServer.Spawn(go);
                }
            }
        }
    }
}
