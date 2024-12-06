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

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);

                if (RunArtifactManager.instance.IsArtifactEnabled(artifact) && self.teamComponent.teamIndex != TeamIndex.Player)
                {
                    if (!disableOnMithrix || self.baseNameToken != "BROTHER_BODY_NAME")
                    {
                        self.moveSpeed *= moveSpeed;
                    }
                    self.attackSpeed *= atkSpeed;
                }
            };

            On.RoR2.Projectile.ProjectileSimple.Start += (orig, self) =>
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
            };
        }
    }
}
