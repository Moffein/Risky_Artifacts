using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Risky_Artifacts.Artifacts
{
    public class Arrogance
    {
        public static ArtifactDef artifact;

        private static int runMountainCount = 0;
        private static int stageMountainCount = 0;

        public Arrogance()
        {
            LanguageAPI.Add("RISKYARTIFACTS_ARROGANCE_NAME", "Artifact of Arrogance");
            LanguageAPI.Add("RISKYARTIFACTS_ARROGANCE_DESC", "The effects of Shrine of the Mountain are permanent.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfArrogance";
            artifact.nameToken = "RISKYARTIFACTS_ARROGANCE_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_ARROGANCE_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texArtifactWarDisabled.png");//todo: unique icon
            artifact.smallIconSelectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texArtifactWarEnabled.png");//todo: unique icon
            ArtifactAPI.Add(artifact);

            On.RoR2.ShrineBossBehavior.AddShrineStack += (orig, self, interactor) =>
            {
                if (NetworkServer.active)
                {
                    stageMountainCount++;
                }
                orig(self, interactor);
            };

            On.RoR2.Run.Start += (orig, self) =>
            {
                runMountainCount = 0;
                stageMountainCount = 0;
                orig(self);
            };

            On.RoR2.Stage.Start += (orig, self) =>
            {
                runMountainCount += stageMountainCount;
                stageMountainCount = 0;
                orig(self);

                if (RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex))
                {
                    if (TeleporterInteraction.instance)
                    {
                        for (int i = 0; i < runMountainCount; i++)
                        {
                            TeleporterInteraction.instance.AddShrineStack();
                        }
                    }
                }
            };
        }
    }
}
