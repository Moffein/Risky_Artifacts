using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace SneedUtils
{
    public static class SneedUtils
    {
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
