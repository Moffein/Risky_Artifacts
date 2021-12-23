using RoR2;
using R2API;
using UnityEngine;
using System.Collections.Generic;

namespace Risky_Artifacts.Artifacts
{
    public class Conformity
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;
        public static bool disableInBazaar = true;
        public static bool enableCleansingPools;
        public Conformity()
        {
            if (!enabled) return;
            LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_NAME", "Artifact of Conformity");
            LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_DESC", "3D Printers and Scrappers no longer spawn.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfConformity";
            artifact.nameToken = "RISKYARTIFACTS_CONFORMITY_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_CONFORMITY_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texConformityResizedDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texConformityResizedEnabled.png");
            ArtifactAPI.Add(artifact);

            DirectorAPI.InteractableActions += delegate (List<DirectorAPI.DirectorCardHolder> cardList, DirectorAPI.StageInfo stage)
            {
                if ((RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex)) && !(stage.stage == DirectorAPI.Stage.Bazaar && disableInBazaar))
                {
                    List<DirectorAPI.DirectorCardHolder> removeList = new List<DirectorAPI.DirectorCardHolder>();
                    foreach (DirectorAPI.DirectorCardHolder dc in cardList)
                    {
                        if (dc.InteractableCategory == DirectorAPI.InteractableCategory.Duplicator)
                        {
                            dc.Card.selectionWeight = 0;
                            removeList.Add(dc);
                        }
                    }

                    foreach (DirectorAPI.DirectorCardHolder dc in removeList)
                    {
                        cardList.Remove(dc);
                    }
                }
            };
        }
    }
}
