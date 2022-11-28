using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Risky_Artifacts.Artifacts.MonoBehaviours
{
    public class BrotherInvasionHauntOnDeath : MonoBehaviour
    {
        public static LunarCoinDef lunarCoinDef = Addressables.LoadAssetAsync<LunarCoinDef>("RoR2/Base/LunarCoin/LunarCoin.asset").WaitForCompletion();
        public static GameObject hauntPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntMaster.prefab").WaitForCompletion();
        public static int lunarCoinsToDrop = 5;

        private HealthComponent healthComponent;
        private bool triggered;
        public void Awake()
        {
            triggered = false;
            healthComponent = base.GetComponent<HealthComponent>();
            if (healthComponent && healthComponent.body)
            {
                healthComponent.body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                healthComponent.body.bodyFlags &= ~CharacterBody.BodyFlags.ImmuneToVoidDeath;
            }

            //Only BrotherHurt has the animation for this.
            /*CharacterDeathBehavior cdb = base.GetComponent<CharacterDeathBehavior>();
            if (cdb)
            {
                cdb.deathState = new EntityStates.SerializableEntityStateType(typeof(EntityStates.BrotherMonster.TrueDeathState));
            }*/
        }

        public void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                if (!triggered)
                {
                    if (healthComponent && !healthComponent.alive)
                    {
                        SpawnHaunt();
                    }
                }
                else
                {
                    Destroy(this);
                }
            }
        }

        public void SpawnHaunt()
        {
            triggered = true;

            if (Run.instance.availableItems.Contains(RoR2Content.Items.ShinyPearl.itemIndex))
            {
                PickupIndex shinyPearlIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex);
                if (shinyPearlIndex != PickupIndex.none)
                {
                    PickupDropletController.CreatePickupDroplet(shinyPearlIndex, base.transform.position, Vector3.up * 20f);
                }

                PickupIndex lunarCoinIndex = PickupCatalog.FindPickupIndex(lunarCoinDef.miscPickupIndex);
                if (lunarCoinIndex != PickupIndex.none)
                {
                    float angle = 360f / (float)BrotherInvasionHauntOnDeath.lunarCoinsToDrop;
                    Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                    Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

                    int k = 0;
                    while (k < BrotherInvasionHauntOnDeath.lunarCoinsToDrop)
                    {
                        PickupDropletController.CreatePickupDroplet(lunarCoinIndex, base.transform.position + 1f*Vector3.up, vector);
                        k++;
                        vector = rotation * vector;
                    }
                }
            }

            MasterSummon summon = new MasterSummon
            {
                useAmbientLevel = true,
                ignoreTeamMemberLimit = true,
                masterPrefab = BrotherInvasionHauntOnDeath.hauntPrefab,
                position = base.transform.position,
                rotation = base.transform.rotation,
                summonerBodyObject = null,
                teamIndexOverride = TeamIndex.Neutral
            };
            summon.Perform();

            Destroy(this);
        }
    }
}
