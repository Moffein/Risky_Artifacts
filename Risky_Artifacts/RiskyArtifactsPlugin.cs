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
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.EliteAPI.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.ItemAPI.PluginGUID)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("zombieseatflesh7.ArtifactOfPotential", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Moffein.RiskyArtifacts", "Risky Artifacts", "2.4.11")]
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
            new Cruelty();
            new Universe();
            new Hunted();

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
            Expansion.priceMult = base.Config.Bind<float>(new ConfigDefinition("Expansion", "Cost Multiplier"), 1f,
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
            Origin.disableParticles = base.Config.Bind<bool>(new ConfigDefinition("Origin", "Disable Particles"), true,
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
            
            BrotherInvasionController.useAdaptiveArmor = base.Config.Bind<bool>(new ConfigDefinition("The Phantom", "Adaptive Armor"), false,
                new ConfigDescription("Enable Adaptive Armor on The Phantom.")).Value;
            BrotherInvasionController.useStatOverride = base.Config.Bind<bool>(new ConfigDefinition("The Phantom", "Stat Override"), false,
                new ConfigDescription("Override Mithrix unique scaling.")).Value;
            BrotherInvasionController.hpMultOverride = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Stat Override - HP Multiplier"), 12f,
                new ConfigDescription("Phantom HP Multiplier if Stat Override is enabled.")).Value;
            BrotherInvasionController.damageMultOverride = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Stat Override - Damage Multiplier"), 3f,
                new ConfigDescription("Phantom Damage Multiplier if Stat Override is enabled.")).Value;

            BrotherInvasionController.minInvasionTimer = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Min Spawn Timer"), 270f,
                new ConfigDescription("Minimum time before the Phantom spawns.")).Value;
            BrotherInvasionController.maxInvasionTimer = base.Config.Bind<float>(new ConfigDefinition("The Phantom", "Max Spawn Timer"), 360f,
                new ConfigDescription("Maximum time before the Phantom spawns.")).Value;
            BrotherInvasionController.minStages = base.Config.Bind<int>(new ConfigDefinition("The Phantom", "Min Stages"), 0,
                new ConfigDescription("Minimum stage completions before the artifact activates.")).Value;


            Cruelty.enabled = base.Config.Bind<bool>(new ConfigDefinition("Cruelty", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;

            Cruelty.guaranteeSpecialBoss = base.Config.Bind<bool>(new ConfigDefinition("Cruelty", "Guarantee Special Boss"), true,
                new ConfigDescription("Always apply Cruelty to special bosses.")).Value;

            Cruelty.runEndBossGeneralAffixes = base.Config.Bind<int>(new ConfigDefinition("Cruelty", "Guarantee Special Boss - Non-T2 Affixes"), 3,
                new ConfigDescription("Elite Types to add to special bosses if Guarantee Special Boss is enabled. Set to 0 to disable tier from being picked. Set to -1 for no limit.")).Value;

            Cruelty.runEndBossT2Affixes = base.Config.Bind<int>(new ConfigDefinition("Cruelty", "Guarantee Special Boss - T2 Affixes"), 0,
                new ConfigDescription("Elite Types to add to special bosses if Guarantee Special Boss is enabled. Set to 0 to disable tier from being picked. Set to -1 for no limit.")).Value;

            Cruelty.triggerChance = base.Config.Bind<float>(new ConfigDefinition("Cruelty", "Trigger Chance"), 25f,
                new ConfigDescription("Chance for Cruelty to be applied to an enemy. Set to 100 to make it always apply.")).Value;
            Cruelty.failureChance = base.Config.Bind<float>(new ConfigDefinition("Cruelty", "Failure Chance"), 25f,
                new ConfigDescription("Chance for Cruelty to stop after each new affix. Set to 0 to make it always attempt to add as many affixes as possible.")).Value;

            Cruelty.costScaling = base.Config.Bind<Cruelty.ScalingMode>(new ConfigDefinition("Cruelty", "Cost Scaling"), Cruelty.ScalingMode.Additive,
                new ConfigDescription("How should director cost scale?")).Value;
            Cruelty.damageScaling = base.Config.Bind<Cruelty.ScalingMode>(new ConfigDefinition("Cruelty", "Damage Scaling"), Cruelty.ScalingMode.None,
                new ConfigDescription("How should elite damage scale?")).Value;
            Cruelty.healthScaling = base.Config.Bind<Cruelty.ScalingMode>(new ConfigDefinition("Cruelty", "Health Scaling"), Cruelty.ScalingMode.Additive,
                new ConfigDescription("How should elite health scale?")).Value;
            Cruelty.rewardScaling = base.Config.Bind<Cruelty.ScalingMode>(new ConfigDefinition("Cruelty", "Reward Scaling"), Cruelty.ScalingMode.Additive,
                new ConfigDescription("How should elite kill rewards scale?")).Value;

            Cruelty.maxT2Affixes = base.Config.Bind<int>(new ConfigDefinition("Cruelty", "Max T2 Affixes"), 1,
                new ConfigDescription("Maximum T2 Affixes that Cruelty can add. Set to 0 to disable tier from being picked. Set to -1 for no limit.")).Value;
            Cruelty.maxGeneralAffixes = base.Config.Bind<int>(new ConfigDefinition("Cruelty", "Max Non-T2 Affixes"), 3,
                new ConfigDescription("Maximum Non-T2 Affixes that Cruelty can add. Set to 0 to disable tier from being picked. Set to -1 for no limit.")).Value;

            Hunted.enabled = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Hunted.allSurvivors = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "All Survivors"), false,
                new ConfigDescription("Allow all survivors to spawn, even if not listed in the Spawnlist.")).Value;
            Hunted.survivorsOnly = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Only Spawn Survivors"), false,
                new ConfigDescription("Only spawn survivors when the artifact is active.")).Value;
            Hunted.spawnInfoInput = base.Config.Bind<string>(new ConfigDefinition("Hunted", "Spawnlist"), "CommandoBody, Bandit2Body, ToolbotBody, MercBody, MageBody, LoaderBody, CrocoBody, RailgunnerBody, SeekerBody, ChefBody, FalseSonBody, RocketSurvivorBody, GnomeChefBody, SniperClassicBody, MinerBody, HANDOverclockedBody, RobPaladinBody, SS2UChirrBody, SS2UCyborgBody, SS2UExecutionerBody, SS2UPyroBody, SS2UNemmandoBody, SS2UNucleatorBody, RobDriverBody, DeputyBody, MoffeinPilotBody, FalseSonBody, ChefBody, SeekerBody",
                new ConfigDescription("List of bodies to be added. Format is BodyName separated by comma. To specify custom stats, do BodyName:Cost:HPMult(float):DamageMult(float)")).Value;
            Hunted.nerfEngiTurrets = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Nerf Engi Turrets"), true,
                new ConfigDescription("Engi Turrets receive no health boost.")).Value;
            Hunted.nerfPercentHeal = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Nerf Percent Heal"), true,
                new ConfigDescription("Percent heal effects are unaffected by the increased HP multiplier.")).Value;
            Hunted.categoryWeight = base.Config.Bind<float>(new ConfigDefinition("Hunted", "Stats - Director Category Weight"), 1f,
                new ConfigDescription("Weight of the Hunted survivor director category. Usually it's 2 for Bosses and Minibosses, 3-4 for Common enemies.")).Value;
            Hunted.healthMult = base.Config.Bind<float>(new ConfigDefinition("Hunted", "Stats - Health Multiplier"), 4f,
                new ConfigDescription("Default health multiplier for Hunted survivors. Vengeance is 10")).Value;
            Hunted.damageMult = base.Config.Bind<float>(new ConfigDefinition("Hunted", "Stats - Damage Multiplier"), 0.2f,
                new ConfigDescription("Default damage multiplier for Hunted survivors. Vengeance is 0.1")).Value;
            Hunted.disableRegen = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Stats - Disable Regen"), true,
                new ConfigDescription("Disable health regeneration for Hunted Survivors.")).Value;
            Hunted.directorCost = base.Config.Bind<int>(new ConfigDefinition("Hunted", "Director Cost"), 160,
                new ConfigDescription("Default director cost for Hunted survivors.")).Value;
            Hunted.useOverlay = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Overlay Texture"), false,
                new ConfigDescription("Hunted survivors use the Vengeance texture.")).Value;
            Hunted.allowCruelty = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Allow Cruelty"), true,
                new ConfigDescription("Hunted survivors can be affected by Cruelty.")).Value;
            Hunted.allowElite = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Allow Elites"), true,
                new ConfigDescription("Hunted survivors can be Elites. Disabling this prevents them from being affected by Cruelty.")).Value;

            Hunted.randomizeSkin = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Randomize Skin"), true,
                new ConfigDescription("Hunted survivors use a random skin.")).Value;

            Hunted.randomizeLoadout = base.Config.Bind<bool>(new ConfigDefinition("Hunted", "Randomize Loadout"), true,
                new ConfigDescription("Hunted survivors use a random loadout.")).Value;

            Universe.enabled = base.Config.Bind<bool>(new ConfigDefinition("Universe", "Enable Artifact"), true,
                new ConfigDescription("Allows this artifact to be selected.")).Value;
            Universe.enableOnMoon = base.Config.Bind<bool>(new ConfigDefinition("Universe", "Replace Moon Spawns"), false,
                new ConfigDescription("Allow Universe to affect Commencement monster spawns.")).Value;
            Universe.enableOnVoidLocus = base.Config.Bind<bool>(new ConfigDefinition("Universe", "Replace Void Locus Spawns"), false,
                new ConfigDescription("Allow Universe to affect Void Locus monster spawns.")).Value;

            Universe.Categories.CatBasicMonsters.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Basic Monsters"), 4f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatMinibosses.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Minibosses"), 2f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatChampions.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Champions"), 2f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatSpecial.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Special"), 0.5f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;

            Universe.Categories.CatMithrix.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Mithrix"), 0.25f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatMithrixHurt.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Mithrix Phase 4"), 0f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatVoidling.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Voidling"), 0.25f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatNewt.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Newt"), 0f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatLunarScav.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Lunar Scavenger"), 0f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;
            Universe.Categories.CatDrone.weight = base.Config.Bind<float>(new ConfigDefinition("Universe - Categories", "Category Weight - Drone"), 0f,
                new ConfigDescription("Chance of this monster category being selected.")).Value;

            Universe.InputInfo.Basic_Monsters = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Basic Monsters"), "ChildBody, LunarExploderBody, BeetleBody, WispBody, LemurianBody, JellyfishBody, HermitCrabBody, VoidBarnacleBody, ImpBody, VultureBody, RoboBallMiniBody, AcidLarvaBody, MinorConstructBody, FlyingVerminBody, VerminBody, MoffeinClayManBody:28",
               new ConfigDescription("List of bodies to be added to this category. Format is BodyName separated by comma. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;
            Universe.InputInfo.Minibosses = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Minibosses"), "SpitterBody:30, ScorchlingBody, LunarGolemBody, LunarWispBody, GupBody, ClayGrenadierBody, ClayBruiserBody, MiniMushroomBody, BisonBody, BellBody, ParentBody, GolemBody, GreaterWispBody, BeetleGuardBody, NullifierBody, VoidJailerBody, LemurianBruiserBody, MoffeinArchWisp:240",
                new ConfigDescription("List of bodies to be added to this category. Format is BodyName separated by comma. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;
            Universe.InputInfo.Champions = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Champions"), "ColossusBody:1000, VagrantBody, TitanBody, BeetleQueen2Body, ClayBossBody, MagmaWormBody, ImpBossBody, RoboBallBossBody, GravekeeperBody, MegaConstructBody, VoidMegaCrabBody, GrandparentBody, ScavBody, ElectricWormBody, MoffeinAncientWispBody:1000, MechorillaBody:600, RegigigasBody:1000",
                new ConfigDescription("List of bodies to be added to this category. Format is BodyName separated by comma. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;
            Universe.InputInfo.Special = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Special"), "TitanGoldBody:6000:5, SuperRoboBallBossBody:6000:5, DireseekerBossBody:6000:5",
               new ConfigDescription("List of bodies to be added to this category. Bodies in this category will receive increased health and damage. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;
            Universe.InputInfo.LunarScav = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Lunar Scavenger"), "ScavLunar1Body:8000:5, ScavLunar2Body:8000: 5,ScavLunar3Body:8000:5, ScavLunar4Body:8000:5",
               new ConfigDescription("List of bodies to be added to this category. Bodies in this category will receive increased health and damage. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;
            Universe.InputInfo.Drone = base.Config.Bind<string>(new ConfigDefinition("Universe - Spawnlists", "Spawnlist - Drone"), "Turret1Body, Drone1Body, Drone2Body, MissileDroneBody, FlameDroneBody, EmergencyDroneBody, MegaDroneBody",
               new ConfigDescription("List of bodies to be added to this category. To specify custom stats, do BodyName:Cost(int):MinStages(int):SelectionWeight(int):NodeGraphType(Default, Air, Ground)")).Value;

            Universe.mithrixCost = base.Config.Bind<int>(new ConfigDefinition("Universe - Mithrix", "Director Cost"), 8000,
                new ConfigDescription("Director cost of Mithrix.")).Value;
            Universe.mithrixMinStages = base.Config.Bind<int>(new ConfigDefinition("Universe - Mithrix", "Min Stages"), 5,
                new ConfigDescription("Min stages completed before Mithrix can spawn.")).Value;
            Universe.mithrixEliteRules = base.Config.Bind<SpawnCard.EliteRules>(new ConfigDefinition("Universe - Mithrix", "Elite Rules"), SpawnCard.EliteRules.ArtifactOnly,
                new ConfigDescription("Affects elite type.")).Value;
            Universe.mithrixAllowElite = base.Config.Bind<bool>(new ConfigDefinition("Universe - Mithrix", "Allow Elite"), true,
                new ConfigDescription("Allow elites to spawn?.")).Value;

            Universe.fsCost = base.Config.Bind<int>(new ConfigDefinition("Universe - False Son", "Director Cost"), 12000,
                new ConfigDescription("Director cost of False Son.")).Value;
            Universe.fsMinStages = base.Config.Bind<int>(new ConfigDefinition("Universe - False Son", "Min Stages"), 5,
                new ConfigDescription("Min stages completed before False Son can spawn.")).Value;
            Universe.fsEliteRules = base.Config.Bind<SpawnCard.EliteRules>(new ConfigDefinition("Universe - False Son", "Elite Rules"), SpawnCard.EliteRules.ArtifactOnly,
                new ConfigDescription("Affects elite type.")).Value;
            Universe.fsAllowElite = base.Config.Bind<bool>(new ConfigDefinition("Universe - False Son", "Allow Elite"), true,
                new ConfigDescription("Allow elites to spawn?.")).Value;

            Universe.mithrixHurtCost = base.Config.Bind<int>(new ConfigDefinition("Universe - Mithrix Phase 4", "Director Cost"), 24000,
                new ConfigDescription("Director cost of Mithrix Phase 4.")).Value;
            Universe.mithrixHurtMinStages = base.Config.Bind<int>(new ConfigDefinition("Universe - Mithrix Phase 4", "Min Stages"), 5,
                new ConfigDescription("Min stages completed before Mithrix Phase 4 can spawn.")).Value;
            Universe.mithrixHurtEliteRules = base.Config.Bind<SpawnCard.EliteRules>(new ConfigDefinition("Universe - Mithrix Phase 4", "Elite Rules"), SpawnCard.EliteRules.ArtifactOnly,
                new ConfigDescription("Affects elite type.")).Value;
            Universe.mithrixHurtAllowElite = base.Config.Bind<bool>(new ConfigDefinition("Universe - Mithrix Phase 4", "Allow Elite"), true,
                new ConfigDescription("Allow elites to spawn?.")).Value;

            Universe.voidlingCost = base.Config.Bind<int>(new ConfigDefinition("Universe - Voidling", "Director Cost"), 12000,
                new ConfigDescription("Director cost of Voidling.")).Value;
            Universe.voidlingMinStages = base.Config.Bind<int>(new ConfigDefinition("Universe - Voidling", "Min Stages"), 5,
                new ConfigDescription("Min stages completed before Voidling can spawn.")).Value;
            Universe.voidlingEliteRules = base.Config.Bind<SpawnCard.EliteRules>(new ConfigDefinition("Universe - Voidling", "Elite Rules"), SpawnCard.EliteRules.ArtifactOnly,
                new ConfigDescription("Affects elite type.")).Value;
            Universe.voidlingPhase2 = base.Config.Bind<bool>(new ConfigDefinition("Universe - Voidling", "Enable Phase 2 Attacks"), true,
                new ConfigDescription("Allow Voidling to use its Phase 2 attacks.")).Value;
            Universe.voidlingAllowElite = base.Config.Bind<bool>(new ConfigDefinition("Universe - Voidling", "Allow Elite"), true,
                new ConfigDescription("Allow elites to spawn?.")).Value;

            Universe.newtCost = base.Config.Bind<int>(new ConfigDefinition("Universe - Newt", "Director Cost"), 18000,
                new ConfigDescription("Director cost of Newt.")).Value;
            Universe.newtMinStages = base.Config.Bind<int>(new ConfigDefinition("Universe - Newt", "Min Stages"), 5,
                new ConfigDescription("Min stages completed before Newt can spawn.")).Value;
            Universe.newtEliteRules = base.Config.Bind<SpawnCard.EliteRules>(new ConfigDefinition("Universe - Newt", "Elite Rules"), SpawnCard.EliteRules.Lunar,
                new ConfigDescription("Affects elite type.")).Value;
            Universe.newtAllowElite = base.Config.Bind<bool>(new ConfigDefinition("Universe - Newt", "Allow Elite"), false,
                new ConfigDescription("Allow elites to spawn?")).Value;

            Universe.droneDamageMult = base.Config.Bind<float>(new ConfigDefinition("Universe - Drone", "Damage Multiplier"), 0.1f,
                new ConfigDescription("Affects the damage stat of enemy drones.")).Value;
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
