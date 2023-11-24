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

        public static List<CharacterSpawnCard> AllSpawnCards = new List<CharacterSpawnCard>();

        public static CharacterSpawnCard AlphaConstruct;

        public static CharacterSpawnCard Beetle;
        public static CharacterSpawnCard Lemurian;

        public static CharacterSpawnCard Wisp;
        public static CharacterSpawnCard Jellyfish;
        public static CharacterSpawnCard BlindPestSnowy;
        public static CharacterSpawnCard BlindVerminSnowy;

        public static CharacterSpawnCard Imp;
        public static CharacterSpawnCard Vulture;

        public static CharacterSpawnCard Golem;
        public static CharacterSpawnCard BeetleGuard;
        public static CharacterSpawnCard Mushrum;
        public static CharacterSpawnCard Bison;
        public static CharacterSpawnCard ClayApothecary;

        public static CharacterSpawnCard Bronzong;
        public static CharacterSpawnCard GreaterWisp;

        public static CharacterSpawnCard TitanBlackBeach;
        public static CharacterSpawnCard TitanDampCave;
        public static CharacterSpawnCard TitanGolemPlains;
        public static CharacterSpawnCard TitanGooLake;

        public static CharacterSpawnCard Vagrant;
        public static CharacterSpawnCard BeetleQueen;
        public static CharacterSpawnCard Dunestrider;

        public static CharacterSpawnCard MagmaWorm;
        public static CharacterSpawnCard ImpOverlord;
        public static CharacterSpawnCard Grovetender;
        public static CharacterSpawnCard RoboBall;
        public static CharacterSpawnCard XiConstruct;

        public static CharacterSpawnCard Reminder;

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            AlphaConstruct = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset").WaitForCompletion();

            Beetle = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetle");
            Lemurian = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csclemurian");

            Wisp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csclesserwisp");
            Jellyfish = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscjellyfish");

            Imp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscimp");
            Vulture = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscvulture");

            Golem = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Golem/cscGolem.asset").WaitForCompletion();
            BeetleGuard = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetleguard");
            Mushrum = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscminimushroom");
            Bison = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Bison/cscBison.asset").WaitForCompletion();

            Bronzong = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbell");
            GreaterWisp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscgreaterwisp");

            TitanBlackBeach = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitanblackbeach");
            TitanDampCave = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitandampcave");
            TitanGolemPlains = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitangolemplains");
            TitanGooLake = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitangoolake");

            Vagrant = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscvagrant");
            BeetleQueen = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetlequeen");
            Dunestrider = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscclayboss");

            MagmaWorm = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscmagmaworm");
            ImpOverlord = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscimpboss");
            Grovetender = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscgravekeeper");
            RoboBall = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscroboballboss");

            Reminder = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscelectricworm");

            BlindVerminSnowy = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/Vermin/cscVerminSnowy.asset").WaitForCompletion();
            BlindPestSnowy = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/FlyingVermin/cscFlyingVerminSnowy.asset").WaitForCompletion();
            ClayApothecary = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/ClayGrenadier/cscClayGrenadier.asset").WaitForCompletion();

            XiConstruct = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset").WaitForCompletion();

            List<CharacterSpawnCard> cards = new List<CharacterSpawnCard>()
            {
                Beetle, Lemurian, Wisp, Jellyfish,
                Imp, Vulture, Golem, BeetleGuard,
                Mushrum, Bison, Bronzong,
                GreaterWisp,
                TitanBlackBeach, TitanDampCave, TitanGolemPlains, TitanGooLake,
                Vagrant, BeetleQueen, Dunestrider, MagmaWorm, ImpOverlord,
                Grovetender, RoboBall,
                Reminder,
                BlindVerminSnowy, BlindPestSnowy, ClayApothecary, XiConstruct
            };
            AllSpawnCards.AddRange(cards);

            Universe_DirectorCards.Init();
        }

        //Only works after BodyCatalog init
        public static CharacterSpawnCard FindCardByBodyname(string bodyName)
        {
            Debug.Log("RiskyArtifacts: Listing cards");
            /*BodyIndex desiredIndex = BodyCatalog.FindBodyIndex(bodyName);
            if (desiredIndex == BodyIndex.None) return null;*/

            foreach (CharacterSpawnCard csc in AllSpawnCards)
            {
                if (csc != null && csc.prefab)
                {
                    Debug.Log(csc.prefab.name);
                }
            }

            return null;
        }
    }

    public static class Universe_DirectorCards
    {
        public static bool initialized = false;

        public static List<DirectorCard> AllDirectorCards = new List<DirectorCard>();

        public static DirectorCard AlphaConstructLoop;

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

            AlphaConstructLoop = BuildDirectorCard(Universe_Spawncards.AlphaConstruct, 1, 5, DirectorCore.MonsterSpawnDistance.Standard);

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
    }
}
