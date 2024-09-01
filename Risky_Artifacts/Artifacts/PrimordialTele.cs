
using UnityEngine;
using RoR2;
using R2API;
using System.Collections.Generic;
using MonoMod.Cil;
using System;
using UnityEngine.Networking;

namespace Risky_Artifacts.Artifacts
{
    public class PrimordialTele
    {
        public static bool enabled = true;
        public static bool enableOnFirstLoop = false;
        public static bool forceEnable = false;
        public static ArtifactDef artifact;
        private static SpawnCard teleCard;
        private static SpawnCard primordialTeleCard;

        public static bool isNaturalLunar = false;
        private static bool firstIdleToActive = true;   //Used for suppressing the chat message?

        public PrimordialTele()
        {
            if (!enabled && !forceEnable) return;

            if (enabled)
            {
                artifact = ScriptableObject.CreateInstance<ArtifactDef>();
                artifact.cachedName = "RiskyArtifactOfPrimacy";
                artifact.nameToken = "RISKYARTIFACTS_PRIMORDIAL_NAME";
                artifact.descriptionToken = "RISKYARTIFACTS_PRIMORDIAL_DESC";
                artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texPrimacyDisabled.png");
                artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texPrimacyEnabled.png");
                RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
                ContentAddition.AddArtifactDef(artifact);
            }

            teleCard = LegacyResourcesAPI.Load<SpawnCard>("spawncards/interactablespawncard/iscTeleporter");
            primordialTeleCard = LegacyResourcesAPI.Load<SpawnCard>("spawncards/interactablespawncard/iscLunarTeleporter");

            LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/teleporters/LunarTeleporterProngs").AddComponent<PrimordialTeleInitialState>();

            IL.RoR2.SceneDirector.PlaceTeleporter += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                     x => x.MatchLdfld<SceneDirector>("teleporterSpawnCard")
                    );
                c.GotoNext(
                     x => x.MatchLdfld<SceneDirector>("teleporterSpawnCard")
                    );
                c.Index++;
                c.EmitDelegate<Func<SpawnCard, SpawnCard>>(origTeleporter =>
                {
                    bool isNormalTele = origTeleporter == teleCard;
                    isNaturalLunar = !isNormalTele;

                    if ((Run.instance.stageClearCount >= 5 || enableOnFirstLoop) && (forceEnable || (enabled && RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex))) && isNormalTele)
                    {
                        origTeleporter = primordialTeleCard;
                    }
                    return origTeleporter;
                });
            };

            On.RoR2.Stage.Start += (orig, self) =>
            {
                firstIdleToActive = true;
                return orig(self);
            };

            //Suppresses the chat message?
            On.EntityStates.LunarTeleporter.IdleToActive.OnExit += (orig, self) =>
            {
                if (!firstIdleToActive || PrimordialTele.isNaturalLunar)
                {
                    firstIdleToActive = false;
                    orig(self);
                }
                else
                {
                    firstIdleToActive = false;
                }
            };
        }
    }

    public class PrimordialTeleInitialState : MonoBehaviour
    {
        public void Start()
        {
            if (NetworkServer.active)
            {
                if (!PrimordialTele.isNaturalLunar)
                {
                    EntityStateMachine esm = base.GetComponent<EntityStateMachine>();
                    esm.SetNextState(new EntityStates.LunarTeleporter.Idle());
                }
            }
            Destroy(this);
        }
    }
}
