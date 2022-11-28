using Mono.Cecil.Cil;
using MonoMod.Cil;
using Risky_Artifacts.Artifacts;
using RoR2;
using System;

namespace Risky_Artifacts
{
    public class HookGetBestBodyName
    {
        public HookGetBestBodyName()
        {
            IL.RoR2.Util.GetBestBodyName += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c = c.GotoNext(x => x.MatchRet());
                c.Emit(OpCodes.Ldloc_0);//CharacterBody
                c.EmitDelegate<Func<string, CharacterBody, string>>((str, cb) =>
                {
                    if (cb.inventory)
                    {
                        if (cb.inventory.GetItemCount(Origin.OriginBonusItem) > 0)
                        {
                            str += Language.GetString("RISKYARTIFACTS_ORIGIN_MODIFIER");
                        }
                        if (cb.inventory.GetItemCount(BrotherInvasion.BrotherInvasionBonusItem) > 0)
                        {
                            str = Language.GetString("RISKYARTIFACTS_BROTHERINVASION_MODIFIER") + str;
                        }
                    }
                    return str;
                });
            };
        }
    }
}
