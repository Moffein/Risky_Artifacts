using RoR2;
using R2API;
using UnityEngine;
using BepInEx;
using Risky_Artifacts.Artifacts;
using System.Reflection;
using BepInEx.Configuration;
using System.Collections.Generic;
using R2API.Utils;
using Risky_Artifacts.Artifacts.MonoBehaviours;
using UnityEngine.AddressableAssets;
using System.Runtime.CompilerServices;

namespace Risky_Artifacts
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("zombieseatflesh7.ArtifactOfPotential", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Moffein.RiskyArtifacts", "Risky Artifacts", "1.9.6")]
    [R2API.Utils.R2APISubmoduleDependency( nameof(RecalculateStatsAPI), nameof(EliteAPI), nameof(ContentAddition), nameof(ItemAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class RiskyArtifactsPlugin : BaseUnityPlugin
    {
        public static bool artifactPotentialLoaded = false;
        public static AssetBundle assetBundle;
        public static GameModeIndex SimulacrumIndex;
        public static PluginInfo pluginInfo;

        public static PickupDropTable tier1Drops = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
        public static PickupDropTable tier2Drops = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
        public static PickupDropTable tier3Drops = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();
        public static GameObject potentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

        public void Awake()
        {
            pluginInfo = Info;
            new LanguageTokens();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Risky_Artifacts.riskyartifactsbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            artifactPotentialLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("zombieseatflesh7.ArtifactOfPotential");

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
            new BrotherInvasion();

            if (Origin.enabled || BrotherInvasion.enabled)
            {
                new HookGetBestBodyName();
            }
        }

        public void ReadConfig()
        {
            Arrogance.enabled = base.Config.Bind<bool>(new ConfigDefinition("Arrogance", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Arrogance.guaranteeMountainShrine = base.Config.Bind<bool>(new ConfigDefinition("Arrogance", "Guarantee Mountain Shrine"), true,
                new ConfigDescription("Guarantees that at least 1 mountain shrine will spawn when this artifact is enabled.")).Value;

            Conformity.enabled = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Conformity.disableInBazaar = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Disable Conformity in Bazaar"), true,
                new ConfigDescription("Allow printers to spawn in the bazaar while Conformity is enabled (for use with mods that do this).")).Value;
            Conformity.removeScrappers = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Remove Scrappers"), true,
                new ConfigDescription("Prevent Scrappers from spawning when this artifact is enabled.")).Value;
            Conformity.removePrinters = base.Config.Bind<bool>(new ConfigDefinition("Conformity", "Remove Printers"), true,
                new ConfigDescription("Prevent Printers from spawning when this artifact is enabled.")).Value;

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
            OriginInvasionManager.invasionInterval = base.Config.Bind<float>(new ConfigDefinition("Origin", "Invasion Interval"), 600f,
                new ConfigDescription("Time in seconds between invasions.")).Value;
            Origin.useAdaptiveArmor = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Boss Adaptive Armor"), false,
                new ConfigDescription("Origin bosses get Mithrix's adaptive armor.")).Value;
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

            Origin.enableRoboBall = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Xi Construct"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;
            Origin.enableWorm = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Void Devastator"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;

            Origin.enableGrandparent = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Grandparent"), false,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;

            Origin.enableScavenger = base.Config.Bind<bool>(new ConfigDefinition("Origin Bosses", "Scavenger"), true,
                new ConfigDescription("Allow this boss to spawn during invasions.")).Value;

            PrimordialTele.enabled = base.Config.Bind<bool>(new ConfigDefinition("Primacy", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            PrimordialTele.enableOnFirstLoop = base.Config.Bind<bool>(new ConfigDefinition("Primacy", "Enable on First Loop"), false,
                new ConfigDescription("Primordial teleporters will spawn on the first loop.")).Value;
            PrimordialTele.forceEnable = base.Config.Bind<bool>(new ConfigDefinition("Primacy", "Force Enable"), false,
                new ConfigDescription("The artifact will always be enabled even in game modes that don't have artifacts.")).Value;

            BrotherInvasion.enabled = base.Config.Bind<bool>(new ConfigDefinition("The Phantom", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            BrotherInvasion.bossLunarTeam = base.Config.Bind<bool>(new ConfigDefinition("The Phantom", "Use Lunar Team"), true,
                new ConfigDescription("The Phantom spawns as part of the Lunar team.")).Value;
            BrotherInvasion.ignoreHonor = base.Config.Bind<bool>(new ConfigDefinition("The Phantom", "Ignore Honor"), false,
                new ConfigDescription("The Phantom isn't forced to be elite if Honor is active.")).Value;

            BrotherInvasionController.minInvasionTimer = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Min Spawn Timer"), 270f,
                new ConfigDescription("Minimum time before the Phantom spawns.")).Value;
            BrotherInvasionController.maxInvasionTimer = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Max Spawn Timer"), 360f,
                new ConfigDescription("Maximum time before the Phantom spawns.")).Value;
            BrotherInvasionController.minStages = base.Config.Bind<int>(new ConfigDefinition("The Phantom", "Min Stages"), 0,
                new ConfigDescription("Minimum stage completions before the artifact activates.")).Value;
        }

        public static void FixScriptableObjectName(ArtifactDef ad)
        {
            (ad as ScriptableObject).name = ad.cachedName;
        }

        public static bool IsPotentialArtifactActive()
        {
            bool isActive = false;
            if (artifactPotentialLoaded) isActive = IsPotentialArtifactActiveInternal();
            return isActive;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool IsPotentialArtifactActiveInternal()
        {
            return RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ArtifactOfPotential.PotentialArtifact.Potential);
        }
    }
}
