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
            //Universe_DirectorCards.Init();

            RoR2Application.onLoad += OnLoad;
        }

        private void OnLoad()
        {
            ParseSpawnlist(Universe.InputInfo.Champions);
        }

        private void ParseSpawnlist(string inputString)
        {
            inputString = new string(inputString.ToCharArray().Where(c => !System.Char.IsWhiteSpace(c)).ToArray());
            string[] splitStages = inputString.Split(',');
            foreach (string str in splitStages)
            {
                string[] current = str.Split(':');

                string name = current[0];

                Universe_Spawncards.CardDict.TryGetValue(name, out CharacterSpawnCard spawnCard);
                if (spawnCard == null)
                {
                    Debug.LogError("RiskyArtifacts: Universe: Could not find spawncard for " + name);
                    continue;
                    //todo: create spawncard
                }

                int cost = spawnCard.directorCreditCost;
                int minStages = 0;

                if (current.Length > 1)
                {
                    for (int i = 1; i < current.Length; i++)
                    {
                        switch (i)
                        {
                            case 1:
                                if (int.TryParse(current[1], out int parsedCost))
                                {
                                    cost = parsedCost;
                                }
                                break;
                            case 2:
                                if (int.TryParse(current[2], out int parsedMinStages))
                                {
                                    minStages = parsedMinStages;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                
                //Todo: Build director card;
            }
        }
    }
}
