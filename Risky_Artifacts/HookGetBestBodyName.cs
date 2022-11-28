using Risky_Artifacts.Artifacts;
using RoR2;

namespace Risky_Artifacts
{
    public class HookGetBestBodyName
    {
        public HookGetBestBodyName()
        {
            On.RoR2.Util.GetBestBodyName += (orig, bodyObject) =>
            {
                string toReturn = orig(bodyObject);
                CharacterBody cb = bodyObject.GetComponent<CharacterBody>();
                if (cb && cb.inventory)
                {
                    if (cb.inventory.GetItemCount(Origin.OriginBonusItem) > 0)
                    {
                        toReturn += Language.GetString("RISKYARTIFACTS_ORIGIN_MODIFIER");
                    }
                    if (cb.inventory.GetItemCount(BrotherInvasion.BrotherInvasionBonusItem) > 0)
                    {
                        toReturn = Language.GetString("RISKYARTIFACTS_BROTHERINVASION_MODIFIER") + toReturn;
                    }
                }
                return toReturn;
            };
        }
    }
}
