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
    [BepInPlugin("com.Moffein.RiskyArtifacts", "Risky Artifacts", "1.4.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(DirectorAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(ItemAPI), nameof(EliteAPI), nameof(ContentAddition))]
    public class RiskyArtifacts : BaseUnityPlugin
    {
        public static AssetBundle assetBundle;
        public static GameModeIndex SimulacrumIndex;
        public void Awake()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Risky_Artifacts.riskyartifactsbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }
            ReadConfig();

            On.RoR2.GameModeCatalog.LoadGameModes += (orig) =>
            {
                orig();
                SimulacrumIndex = GameModeCatalog.FindGameModeIndex("InfiniteTowerRun");
            };

            new Warfare();
            new Conformity();
            new Arrogance();
            new Expansion();
            new Origin();
            new PrimordialTele();
        }

        public void ReadConfig()
        {
            Arrogance.enabled = base.Config.Bind<bool>(new ConfigDefinition("Arrogance", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;

            Conformity.enabled = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Conformity.disableInBazaar = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Disable Conformity in Bazaar"), true,
                new ConfigDescription("Allow printers to spawn in the bazaar while Conformity is enabled (for use with mods that do this).")).Value;

            Warfare.enabled = base.Config.Bind<bool>(new ConfigDefinition("Warfare", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Warfare.moveSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Move Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy movement speed.")).Value;
            Warfare.atkSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Attack Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy attack speed.")).Value;
            Warfare.projSpeed = base.Config.Bind<float>(new ConfigDefinition("Warfare", "Projectile Speed Multiplier"), 1.5f,
                new ConfigDescription("Multiplier for enemy projectile speed.")).Value;
            Warfare.disableOnMithrix = base.Config.Bind<bool>(new ConfigDefinition("Warfare", "Disable move speed boost for Michael"), true,
                new ConfigDescription("Makes Michael unaffected by the move speed boost of this artifact because it causes him to always miss his melee.")).Value;

            Expansion.enabled = base.Config.Bind<bool>(new ConfigDefinition("Expansion", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Expansion.priceMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Cost Multiplier"), 1.2f,
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

            Origin.enabled = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Origin.bossVoidTeam = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Use Void Team"), true,
                new ConfigDescription("Bosses spawn as part of the Void team.")).Value;
            Origin.combineSpawns = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Combine Spawns"), true,
                new ConfigDescription("Combine spawns into elites if too many bosses are spawning.")).Value;
            Origin.ignoreHonor = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Ignore Honor"), false,
                new ConfigDescription("Invasion spawns aren't forced to be elite if Honor is active.")).Value;
            Origin.impOnly = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Imps Only"), false,
                new ConfigDescription("Only Imp Overlords spawn during invasions.")).Value;
            Origin.disableParticles = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Disable Particles"), false,
                new ConfigDescription("Disables the Origin Boss particle effect.")).Value;


            Origin.enableTitan = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Stone Titan"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableVagrant = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Wandering Vagrant"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableDunestrider = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Clay Dunestrider"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableBeetle = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Beetle Queen"), false,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;

            Origin.enableImp = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Imp Overlord"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableGrovetender = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Grovetender"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableRoboBall = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Solus Control Unit"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableWorm = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Magma Worm"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.allowEliteWorms = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Magma Worm - Allow Elites"), false,
                new ConfigDescription("Allow this boss to spawn as an elite.")).Value;

            Origin.enableGrandparent = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Grandparent"), false,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;

            PrimordialTele.enabled = base.Config.Bind<bool>(new ConfigDefinition("Primacy", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
        }
    }
}
