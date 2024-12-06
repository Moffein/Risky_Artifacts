using R2API;
using RoR2;
using UnityEngine;

namespace Risky_Artifacts.Artifacts
{
    public class Warfare
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;

        public static bool disableOnMithrix = true;
        public static float moveSpeed = 1.5f;
        public static float atkSpeed = 1.5f;
        public static float projSpeed = 1.5f;
        public Warfare()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfWarfare";
            artifact.nameToken = "RISKYARTIFACTS_WARFARE_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_WARFARE_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texArtifactWarDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texArtifactWarEnabled.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.Projectile.ProjectileSimple.Start += ProjectileSimple_Start;
        }

        private void ProjectileSimple_Start(On.RoR2.Projectile.ProjectileSimple.orig_Start orig, RoR2.Projectile.ProjectileSimple self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artifact) && self.rigidbody && !self.rigidbody.useGravity)
            {
                TeamFilter tf = self.gameObject.GetComponent<TeamFilter>();
                if (tf && tf.teamIndex != TeamIndex.Player)
                {
                    self.desiredForwardSpeed *= projSpeed;
                }
            }
            orig(self);
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artifact) && sender.teamComponent && sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                if (!disableOnMithrix || sender.baseNameToken != "BROTHER_BODY_NAME")
                {
                    args.moveSpeedMultAdd += moveSpeed - 1f;
                }
                args.attackSpeedMultAdd += atkSpeed - 1f;
            }
        }
    }
}
