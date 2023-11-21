using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace Risky_Artifacts
{
    public static class Utils
    {
        //https://stackoverflow.com/questions/273313/randomize-a-list
        private static System.Random rng = new System.Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static int CompareEliteTierCost(CombatDirector.EliteTierDef tier1, CombatDirector.EliteTierDef tier2)
        {
            if (tier1.costMultiplier == tier2.costMultiplier)
            {
                return 0;
            }
            else
            {
                if (tier1.costMultiplier > tier2.costMultiplier)
                {
                    return -1;
                }
                return 1;
            }
        }

        public static EliteDef GetRandomElite(CombatDirector.EliteTierDef et)
        {
            return et.eliteTypes[UnityEngine.Random.Range(0, et.eliteTypes.Length)];
        }

        public static Vector3 FindSafeTeleportPosition(GameObject gameObject, Vector3 targetPosition)
        {
            return FindSafeTeleportPosition(gameObject, targetPosition, float.NegativeInfinity, float.NegativeInfinity);
        }

        public static Vector3 FindSafeTeleportPosition(GameObject gameObject, Vector3 targetPosition, float idealMinDistance, float idealMaxDistance)
        {
            Vector3 vector = targetPosition;
            SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
            spawnCard.hullSize = HullClassification.Human;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
            Vector3 result = vector;
            GameObject teleportGameObject = null;
            if (idealMaxDistance > 0f && idealMinDistance < idealMaxDistance)
            {
                teleportGameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    minDistance = idealMinDistance,
                    maxDistance = idealMaxDistance,
                    position = vector
                }, RoR2Application.rng));
            }
            if (!teleportGameObject)
            {
                teleportGameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                    position = vector
                }, RoR2Application.rng));
                if (teleportGameObject)
                {
                    result = teleportGameObject.transform.position;
                }
            }
            if (teleportGameObject)
            {
                UnityEngine.Object.Destroy(teleportGameObject);
            }
            UnityEngine.Object.Destroy(spawnCard);
            return result;
        }
    }
}
