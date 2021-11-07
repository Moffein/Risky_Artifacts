using RoR2;
using R2API;
using UnityEngine;
using BepInEx;
using Risky_Artifacts.Artifacts;
using System.Reflection;
using BepInEx.Configuration;

namespace Risky_Artifacts
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Moffein.RiskyArtifacts", "Risky Artifacts", "1.0.6")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(DirectorAPI), nameof(ArtifactAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(ItemAPI))]
    public class RiskyArtifacts : BaseUnityPlugin
    {
        public static AssetBundle assetBundle;
        public void Awake()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Risky_Artifacts.riskyartifactsbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }
            ReadConfig();

            new Warfare();
            new Conformity();
            new Arrogance();
            new Expansion();
            new Origin();
        }

        public void ReadConfig()
        {
            Conformity.disableInBazaar = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Disable Conformity in Bazaar"), true,
                new ConfigDescription("Allow printers to spawn in the bazaar while Conformity is enabled (for use with mods that do this).")).Value;

            Warfare.moveSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Move Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy movement speed.")).Value;
            Warfare.atkSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Attack Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy attack speed.")).Value;
            Warfare.projSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Projectile Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy projectile speed.")).Value;
            Warfare.disableOnMithrix = base.Config.Bind<bool>(new ConfigDefinition("Warfare", "Disable move speed boost for Michael"), true,
                new ConfigDescription("Makes Michael unaffected by the move speed boost of this artifact because it causes him to always miss his melee.")).Value;

            Expansion.priceMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Cost Multiplier"), 1.3f,
                new ConfigDescription("Multiplier for how much money things cost.")).Value;
            Expansion.teleRadiusMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Teleporter Radius Multiplier"), 10000f,
                new ConfigDescription("Multiplier for radius size.")).Value;
            Expansion.teleDurationMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Teleporter Duration Multiplier"), 4f / 3f,
                new ConfigDescription("Multiplier for charge duration.")).Value;
            Expansion.voidRadiusMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Void Radius Multiplier"), 2f,
                new ConfigDescription("Multiplier for radius size.")).Value;
            Expansion.voidDurationMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Void Duration Multiplier"), 4f / 3f,
                new ConfigDescription("Multiplier for charge duration.")).Value;
            Expansion.moonRadiusMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Moon Radius Multiplier"), 2f,
                new ConfigDescription("Multiplier for radius size.")).Value;
            Expansion.moonDurationMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Moon Duration Multiplier"), 4f / 3f,
                new ConfigDescription("Multiplier for charge duration.")).Value;

            Origin.impOnly = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Imps Only"), false,
                new ConfigDescription("Only Imp Overlords spawn during invasions.")).Value;
        }
    }
}
