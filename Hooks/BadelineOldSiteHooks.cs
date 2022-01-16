// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BadelineOldSiteHooks
    { 

        public static Dictionary<Player.ChaserState, GravityType> ChaserStateGravity; //Used for determining when the BadelineChaser. Is set in Player.ctor so this shouldn't be a problem ever.

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BadelineOldsite)} hooks...");

            IL.Celeste.BadelineOldsite.Update += BadelineOldsite_Update;

        }

        private static void BadelineOldsite_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            VariableDefinition chaserState = il.Body.Variables.FirstOrDefault(e => e.VariableType.FullName == "Celeste.Player/ChaserState");
            if (chaserState == default(VariableDefinition) || !cursor.TryGotoNext(MoveType.After, instr=>instr.MatchStfld<Monocle.Entity>("Position")))
                throw new HookException("hook point not found for BadelineOldSite Gravity modifier, or, chaserState variable not found in BadelineOldSite.Update().");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, chaserState);
            cursor.EmitDelegate<Action<BadelineOldsite, Player.ChaserState>>((b, pcs) =>
            {
                if (ChaserStateGravity.TryGetValue(pcs, out GravityType gT))
                    BadelineOldsiteModifyForGravity(b, gT);
            });

        });

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BadelineOldsite)} hooks...");
            IL.Celeste.BadelineOldsite.Update -= BadelineOldsite_Update;
        }

        private static void BadelineOldsiteModifyForGravity(BadelineOldsite baddy, GravityType gravityType)
        {

            if(gravityType == GravityType.Normal)
            {
                baddy.Collider = new Hitbox(6f, 6f, -3f, -7f);
                baddy.Sprite.Scale.Y = 1f;
            }
            else if(gravityType == GravityType.Inverted)
            {
                baddy.Collider = new Hitbox(6f, 6f, -3f, 1f);
                baddy.Sprite.Scale.Y = -1f;
            }
        }
    }
}
