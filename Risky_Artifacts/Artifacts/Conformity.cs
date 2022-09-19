using RoR2;
using R2API;
using UnityEngine;
using System.Collections.Generic;

namespace Risky_Artifacts.Artifacts
{
    public class Conformity
    {
        public static bool enabled = true;
        public static bool removeScrappers = true;
        public static bool removePrinters = true;
        public static bool disableInBazaar = true;
        public static ArtifactDef artifact;

        public Conformity()
        {
            if (!enabled) return;
            LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_NAME", "Artifact of Conformity");

            if (removeScrappers && !removePrinters)
            {
                LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_DESC", "Scrappers no longer spawn.");
            }
            else if (removePrinters && !removeScrappers)
            {
                LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_DESC", "3D Printers no longer spawn.");
            }
            else
            {
                LanguageAPI.Add("RISKYARTIFACTS_CONFORMITY_DESC", "3D Printers and Scrappers no longer spawn.");
            }

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfConformity";
            artifact.nameToken = "RISKYARTIFACTS_CONFORMITY_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_CONFORMITY_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texConformityResizedDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifacts.assetBundle.LoadAsset<Sprite>("texConformityResizedEnabled.png");
            RiskyArtifacts.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            if (removeScrappers)
            {
                On.EntityStates.Scrapper.ScrapperBaseState.OnEnter += (orig, self) =>
                {
                    orig(self);
                    if (RunArtifactManager.instance.IsArtifactEnabled(artifact))
                    {
                        Debug.Log("Conformity - Removing Scrapper");

                        SceneDef sd = RoR2.SceneCatalog.GetSceneDefForCurrentScene();
                        if (!(disableInBazaar && sd && sd.baseSceneName.Equals("bazaar")))
                        {
                            UnityEngine.Object.Destroy(self.gameObject);
                        }
                    }
                };
            }

            if (removePrinters)
            {
                On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
                {
                    orig(self);
                    if (RunArtifactManager.instance.IsArtifactEnabled(artifact) && (self.displayNameToken == "DUPLICATOR_NAME" || self.displayNameToken == "DUPLICATOR_WILD_NAME" || self.displayNameToken == "DUPLICATOR_MILITARY_NAME"))
                    {
                        Debug.Log("Conformity - Removing Printer");

                        SceneDef sd = RoR2.SceneCatalog.GetSceneDefForCurrentScene();
                        if (!(disableInBazaar && sd && sd.baseSceneName.Equals("bazaar")))
                        {
                            UnityEngine.Object.Destroy(self.gameObject);
                        }
                    }
                };
            }
        }
    }
}
