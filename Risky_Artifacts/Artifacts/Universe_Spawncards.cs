using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Risky_Artifacts.Artifacts
{
    public static class Universe_Spawncards
    {
        public static bool initialized = false;

        //It would be better to to based on Mastername, but Hunted was built to be based on BodyName so now this has to do that for consistency.
        //But Hunted should have been based on Mastername as well in retrospect.
        public static Dictionary<string, CharacterSpawnCard> CardDict = new Dictionary<string, CharacterSpawnCard>();

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            //Common
            AddDictEntry("VerminBody", LoadCSC("RoR2/DLC1/Vermin/cscVermin.asset"));
            AddDictEntry("FlyingVerminBody", LoadCSC("RoR2/DLC1/FlyingVermin/cscFlyingVermin.asset"));
            AddDictEntry("MinorConstructBody", LoadCSC("RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset"));
            AddDictEntry("AcidLarvaBody", LoadCSC("RoR2/DLC1/AcidLarva/cscAcidLarva.asset"));
            AddDictEntry("GipBody", LoadCSC("RoR2/DLC1/Gup/cscGipBody.asset"));
            AddDictEntry("RoboBallMiniBody", LoadCSC("RoR2/Base/RoboBallBoss/cscRoboBallMini.asset"));
            AddDictEntry("ImpBody", LoadCSC("RoR2/Base/Imp/cscImp.asset"));
            AddDictEntry("VultureBody", LoadCSC("RoR2/Base/Vulture/cscVulture.asset"));
            AddDictEntry("JellyfishBody", LoadCSC("RoR2/Base/Jellyfish/cscJellyfish.asset"));
            AddDictEntry("WispBody", LoadCSC("RoR2/Base/Wisp/cscLesserWisp.asset"));
            AddDictEntry("BeetleBody", LoadCSC("RoR2/Base/Beetle/cscBeetle.asset"));
            AddDictEntry("HermitCrabBody", LoadCSC("RoR2/Base/HermitCrab/cscHermitCrab.asset"));
            AddDictEntry("LemurianBody", LoadCSC("RoR2/Base/Lemurian/cscLemurian.asset"));
            AddDictEntry("VoidBarnacleBody", LoadCSC("RoR2/DLC1/VoidBarnacle/cscVoidBarnacle.asset"));

            //Miniboss
            AddDictEntry("LunarExploderBody", LoadCSC("RoR2/Base/LunarExploder/cscLunarExploder.asset"));
            AddDictEntry("LunarGolemBody", LoadCSC("RoR2/Base/LunarGolem/cscLunarGolem.asset"));
            AddDictEntry("LunarWispBody", LoadCSC("RoR2/Base/LunarWisp/cscLunarWisp.asset"));
            AddDictEntry("GupBody", LoadCSC("RoR2/DLC1/Gup/cscGupBody.asset"));
            AddDictEntry("GeepBody", LoadCSC("RoR2/DLC1/Gup/cscGeepBody.asset"));
            AddDictEntry("ClayGrenadierBody", LoadCSC("RoR2/DLC1/ClayGrenadier/cscClayGrenadier.asset"));
            AddDictEntry("ClayBruiserBody", LoadCSC("RoR2/Base/ClayBruiser/cscClayBruiser.asset"));
            AddDictEntry("MiniMushroomBody", LoadCSC("RoR2/Base/MiniMushroom/cscMiniMushroom.asset"));
            AddDictEntry("BisonBody", LoadCSC("RoR2/Base/Bison/cscBison.asset"));
            AddDictEntry("BellBody", LoadCSC("RoR2/Base/Bell/cscBell.asset"));
            AddDictEntry("ParentBody", LoadCSC("RoR2/Base/Parent/cscParent.asset"));
            AddDictEntry("GolemBody", LoadCSC("RoR2/Base/Golem/cscGolem.asset"));
            AddDictEntry("GreaterWispBody", LoadCSC("RoR2/Base/GreaterWisp/cscGreaterWisp.asset"));
            AddDictEntry("BeetleGuardBody", LoadCSC("RoR2/Base/Beetle/cscBeetleGuard.asset"));
            AddDictEntry("NullifierBody", LoadCSC("RoR2/Base/Nullifier/cscNullifier.asset"));
            AddDictEntry("VoidJailerBody", LoadCSC("RoR2/DLC1/VoidJailer/cscVoidJailer.asset"));
            AddDictEntry("LemurianBruiserBody", LoadCSC("RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset"));

            //Champions
            AddDictEntry("TitanBody", LoadCSC("RoR2/Base/Titan/cscTitanGolemPlains.asset"));
            AddDictEntry("ClayBossBody", LoadCSC("RoR2/Base/ClayBoss/cscClayBoss.asset"));
            AddDictEntry("BeetleQueen2Body", LoadCSC("RoR2/Base/Beetle/cscBeetleQueen.asset"));
            AddDictEntry("VagrantBody", LoadCSC("RoR2/Base/Vagrant/cscVagrant.asset"));

            AddDictEntry("MagmaWormBody", LoadCSC("RoR2/Base/MagmaWorm/cscMagmaWorm.asset"));
            AddDictEntry("ImpBossBody", LoadCSC("RoR2/Base/ImpBoss/cscImpBoss.asset"));
            AddDictEntry("GravekeeperBody", LoadCSC("RoR2/Base/Gravekeeper/cscGravekeeper.asset"));
            AddDictEntry("RoboBallBossBody", LoadCSC("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset"));
            AddDictEntry("MegaConstructBody", LoadCSC("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset"));
            AddDictEntry("VoidMegaCrabBody", LoadCSC("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset"));

            AddDictEntry("GrandparentBody", LoadCSC("RoR2/Base/Grandparent/cscGrandparent.asset"));
            AddDictEntry("ScavBody", LoadCSC("RoR2/Base/Scav/cscScav.asset"));
            AddDictEntry("ElectricWormBody", LoadCSC("RoR2/Base/ElectricWorm/cscElectricWorm.asset"));
        }

        public static void AddDictEntry(string bodyname, CharacterSpawnCard spawnCard)
        {
            if (spawnCard == null)
            {
                Debug.LogError("RiskyArtifacts: Universe: Null spawncard for " + bodyname);
                return;
            }

            CardDict.Add(bodyname, spawnCard);
        }

        private static CharacterSpawnCard LoadCSC(string path)
        {
            return Addressables.LoadAssetAsync<CharacterSpawnCard>(path).WaitForCompletion();
        }
    }

    /*public static class Universe_DirectorCards
    {
        public static bool initialized = false;

        public static List<DirectorCard> AllDirectorCards = new List<DirectorCard>();

        //public static DirectorCard AlphaConstructLoop;

        public static DirectorCard Beetle;
        public static DirectorCard Lemurian;

        public static DirectorCard Wisp;
        public static DirectorCard Jellyfish;
        public static DirectorCard BlindPestSnowy;
        public static DirectorCard BlindVerminSnowy;

        public static DirectorCard Imp;
        public static DirectorCard Vulture;

        public static DirectorCard Golem;
        public static DirectorCard BeetleGuard;
        public static DirectorCard Mushrum;
        public static DirectorCard ClayApothecary;
        public static DirectorCard Bison;
        public static DirectorCard BisonLoop;

        public static DirectorCard Bronzong;
        public static DirectorCard GreaterWisp;

        public static DirectorCard TitanBlackBeach;
        public static DirectorCard TitanDampCave;
        public static DirectorCard TitanGolemPlains;
        public static DirectorCard TitanGooLake;

        public static DirectorCard Vagrant;
        public static DirectorCard BeetleQueen;
        public static DirectorCard Dunestrider;

        public static DirectorCard MagmaWorm;
        public static DirectorCard MagmaWormLoop;
        public static DirectorCard ImpOverlord;
        public static DirectorCard Grovetender;
        public static DirectorCard RoboBall;

        public static DirectorCard Reminder;
        public static DirectorCard ReminderLoop;

        public static DirectorCard LunarGolemSkyMeadow;
        public static DirectorCard LunarGolemSkyMeadowBasic;

        public static bool logCardInfo = false;
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            //AlphaConstructLoop = BuildDirectorCard(Universe_Spawncards.AlphaConstruct, 1, 5, DirectorCore.MonsterSpawnDistance.Standard);

            Beetle = BuildDirectorCard(Universe_Spawncards.Beetle);
            Lemurian = BuildDirectorCard(Universe_Spawncards.Lemurian);

            Wisp = BuildDirectorCard(Universe_Spawncards.Wisp);
            Jellyfish = BuildDirectorCard(Universe_Spawncards.Jellyfish, 1, 0, DirectorCore.MonsterSpawnDistance.Far);
            BlindPestSnowy = BuildDirectorCard(Universe_Spawncards.BlindPestSnowy);
            BlindVerminSnowy = BuildDirectorCard(Universe_Spawncards.BlindVerminSnowy);

            Imp = BuildDirectorCard(Universe_Spawncards.Imp);
            Vulture = BuildDirectorCard(Universe_Spawncards.Vulture);

            Golem = BuildDirectorCard(Universe_Spawncards.Golem);
            BeetleGuard = BuildDirectorCard(Universe_Spawncards.BeetleGuard);
            Mushrum = BuildDirectorCard(Universe_Spawncards.Mushrum); //These are considered basic monsters in Vanilla, but they fit all the criteria of a miniboss enemy.
            ClayApothecary = BuildDirectorCard(Universe_Spawncards.ClayApothecary);
            Bison = BuildDirectorCard(Universe_Spawncards.Bison);
            BisonLoop = BuildDirectorCard(Universe_Spawncards.Bison, 1, 5, DirectorCore.MonsterSpawnDistance.Standard);

            Bronzong = BuildDirectorCard(Universe_Spawncards.Bronzong);  //Basic Monster on SkyMeadow
            GreaterWisp = BuildDirectorCard(Universe_Spawncards.GreaterWisp);

            TitanBlackBeach = BuildDirectorCard(Universe_Spawncards.TitanBlackBeach);
            TitanDampCave = BuildDirectorCard(Universe_Spawncards.TitanDampCave);
            TitanGolemPlains = BuildDirectorCard(Universe_Spawncards.TitanGolemPlains);
            TitanGooLake = BuildDirectorCard(Universe_Spawncards.TitanGooLake);

            Vagrant = BuildDirectorCard(Universe_Spawncards.Vagrant);
            BeetleQueen = BuildDirectorCard(Universe_Spawncards.BeetleQueen);
            Dunestrider = BuildDirectorCard(Universe_Spawncards.Dunestrider);

            ImpOverlord = BuildDirectorCard(Universe_Spawncards.ImpOverlord);
            Grovetender = BuildDirectorCard(Universe_Spawncards.Grovetender);
            RoboBall = BuildDirectorCard(Universe_Spawncards.RoboBall);
            MagmaWorm = BuildDirectorCard(Universe_Spawncards.MagmaWorm);
            MagmaWormLoop = BuildDirectorCard(Universe_Spawncards.MagmaWorm, 1, 5, DirectorCore.MonsterSpawnDistance.Standard);

            Reminder = BuildDirectorCard(Universe_Spawncards.Reminder);
            ReminderLoop = BuildDirectorCard(Universe_Spawncards.Reminder, 1, 5, DirectorCore.MonsterSpawnDistance.Standard);
        }

        public static DirectorCard BuildDirectorCard(CharacterSpawnCard spawnCard)
        {
            return BuildDirectorCard(spawnCard, 1, 0, DirectorCore.MonsterSpawnDistance.Standard);
        }

        public static DirectorCard BuildDirectorCard(CharacterSpawnCard spawnCard, int weight, int minStages, DirectorCore.MonsterSpawnDistance spawnDistance)
        {
            DirectorCard dc = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = weight,
                preventOverhead = false,
                minimumStageCompletions = minStages,
                spawnDistance = spawnDistance,
                forbiddenUnlockableDef = null,
                requiredUnlockableDef = null
            };
            return dc;
        }
    }*/
}
