﻿using RoR2;
using R2API;
using UnityEngine;

namespace Risky_Artifacts.Artifacts
{
    public class Expansion
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;

        public static float teleRadiusMult = 10000f;
        public static float teleDurationMult = 4f/3f;

        public static float voidRadiusMult = 2f;
        public static float voidDurationMult = 4f / 3f;

        public static float moonRadiusMult = 2f;
        public static float moonDurationMult = 4f / 3f;

        public static float priceMult = 1.2f;

        public Expansion()
        {
            if (!enabled) return;

            string desc = priceMult > 1f ? "RISKYARTIFACTS_EXPANSION_DESC" : "RISKYARTIFACTS_EXPANSION_DESC_NOPRICEINCREASE";
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfExpansion";
            artifact.nameToken = "RISKYARTIFACTS_EXPANSION_NAME";
            artifact.descriptionToken = desc;
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texExpansionDisabled.png");
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texExpansionEnabled.png");
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            On.RoR2.Run.IsItemAvailable += (orig, self, itemIndex) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex))
                {
                    return itemIndex != RoR2Content.Items.FocusConvergence.itemIndex && orig(self, itemIndex);
                }
                else
                {
                    return orig(self, itemIndex);
                }
            };

            On.RoR2.HoldoutZoneController.Awake += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex) && self && Run.instance.gameModeIndex != RiskyArtifactsPlugin.SimulacrumIndex)
                {
                    SceneDef sd = RoR2.SceneCatalog.GetSceneDefForCurrentScene();
                    if (sd && (sd.baseSceneName.Equals("arena") || sd.baseSceneName.Equals("voidstage")))
                    {
                        self.baseRadius *= voidRadiusMult;
                        self.baseChargeDuration *= voidDurationMult;
                    }
                    else if (sd && sd.baseSceneName.Equals("moon2") && self.name.Contains("MoonBattery"))
                    {
                        self.baseRadius *= moonRadiusMult;
                        self.baseChargeDuration *= moonDurationMult;
                    }
                    else if (self.name == "LunarTeleporter Variant(Clone)" || self.name == "Teleporter1(Clone)")
                    {
                        self.baseRadius *= teleRadiusMult;
                        self.baseChargeDuration *= teleDurationMult;
                    }
                }
                orig(self);
            };

            if (priceMult != 1f)
            {
                On.RoR2.Run.GetDifficultyScaledCost_int_float += (orig, self, baseCost, difficultyCoefficient) =>
                {
                    return (int)(orig(self, baseCost, difficultyCoefficient) * (RunArtifactManager.instance.IsArtifactEnabled(artifact.artifactIndex) ? priceMult : 1f));
                };
            }
        }
    }
}
