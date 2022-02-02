// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BadelineOldsiteHooks
    {
        // Used for determining when the BadelineChaser. Is set in Player.ctor so this shouldn't be a problem ever.
        public static readonly Dictionary<int, GravityType> ChaserStateGravity = new();

        public static GravityType GravityTypeForState(float timestamp, GravityType defaultValue = GravityType.Normal)
        {
            var key = (int)(timestamp * 1000);
            return ChaserStateGravity.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static bool TryGravityTypeForState(float timestamp, out GravityType value)
        {
            var key = (int)(timestamp * 1000);
            return ChaserStateGravity.TryGetValue(key, out value);
        }

        public static void SetGravityTypeForState(float timestamp, GravityType value)
        {
            var key = (int)(timestamp * 1000);
            ChaserStateGravity[key] = value;
        }

        public static void RemoveGravityTypeForState(float timestamp)
        {
            var key = (int)(timestamp * 1000);
            ChaserStateGravity.Remove(key);
        }

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BadelineOldsite)} hooks...");
            IL.Celeste.BadelineOldsite.Update += BadelineOldsite_Update;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BadelineOldsite)} hooks...");
            IL.Celeste.BadelineOldsite.Update -= BadelineOldsite_Update;
        }

        private static void BadelineOldsite_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            VariableDefinition chaserState = il.Body.Variables.FirstOrDefault(e => e.VariableType.FullName == "Celeste.Player/ChaserState");
            if (chaserState == null)
                throw new HookException("chaserState variable not found in BadelineOldsite.Update().");

            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Entity>("Position")))
                throw new HookException("hook point not found for BadelineOldsite Gravity modifier.");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, chaserState);
            cursor.EmitDelegate<Action<BadelineOldsite, Player.ChaserState>>((self, pcs) =>
            {
                if (TryGravityTypeForState(pcs.TimeStamp, out var gravityType))
                    badelineOldsiteModifyForGravity(self, gravityType);
            });
        });

        private static void badelineOldsiteModifyForGravity(BadelineOldsite baddy, GravityType gravityType)
        {
            var invert = gravityType == GravityType.Inverted;
            baddy.Collider = new Hitbox(6f, 6f, -3f, invert ? 1f : -7f);
            baddy.SetShouldInvert(invert);
        }
    }
}
