using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Risky_Artifacts.Artifacts
{
    public class Universe
    {
        public static bool enabled = true;
        public static ArtifactDef artifact;

        public static class InputInfo
        {
            public static string Basic_Monsters, Minibosses, Champions;
        }

        public static class DirectorCards
        {
            public static List<DirectorCard> Basic_Monsters = new List<DirectorCard>();
            public static List<DirectorCard> Minibosses = new List<DirectorCard>();
            public static List<DirectorCard> Champions = new List<DirectorCard>();
        }

        public class DirectorInfo
        {
            public string bodyName;
            public BodyIndex bodyIndex;
            public MonsterCategory monsterCategory = MonsterCategory.Basic_Monsters;
            public int directorCost = -1;
        }

        public enum MonsterCategory
        {
            Basic_Monsters, Minibosses, Champions, Special
        }

        public Universe()
        {
            if (!enabled) return;
            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "RiskyArtifactOfUniverse";
            artifact.nameToken = "RISKYARTIFACTS_UNIVERSE_NAME";
            artifact.descriptionToken = "RISKYARTIFACTS_UNIVERSE_DESC";
            artifact.smallIconDeselectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedDisabled.png");//todo
            artifact.smallIconSelectedSprite = RiskyArtifactsPlugin.assetBundle.LoadAsset<Sprite>("texHuntedEnabled.png");//todo
            RiskyArtifactsPlugin.FixScriptableObjectName(artifact);
            ContentAddition.AddArtifactDef(artifact);

            //Just copied from RiskyMod
            Universe_Spawncards.Init();
            Universe_DirectorCards.Init();

            RoR2.RoR2Application.onLoad += OnLoad;
        }

        private void OnLoad()
        {
            Universe_Spawncards.FindCardByBodyname("CommandoBody");
        }

        public static DirectorCard GenerateDirectorCard(DirectorInfo info)
        {
            Debug.Log("RiskyArtifacts: Universe: Creating card for " + info.bodyIndex + " - " + info.bodyName);
            GameObject master = MasterCatalog.GetMasterPrefab(MasterCatalog.FindAiMasterIndexForBody(info.bodyIndex));
            if (!master)
            {
                Debug.LogError("RiskyArtifacts: Universe: Master could not be found for body " + info.bodyIndex + " - " + info.bodyName);
            }
            return null;
        }
    }
}
