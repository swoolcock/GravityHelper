// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal static class JumpThruHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.MoveVExact += JumpThru_MoveVExact;
            IL.Celeste.JumpThru.MoveVExact += JumpThru_MoveVExact;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.MoveVExact -= JumpThru_MoveVExact;
            IL.Celeste.JumpThru.MoveVExact -= JumpThru_MoveVExact;
        }

        private static void JumpThru_MoveVExact(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1)))
                throw new HookException("Couldn't find if (moveV < 0)");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<int, JumpThru, int>>((move, self) =>
                self.IsUpsideDownJumpThru() ? -move : move);
        });

        private static void JumpThru_MoveVExact(On.Celeste.JumpThru.orig_MoveVExact orig, JumpThru self, int move)
        {
            GravityHelperModule.OverrideSemaphore++;
            orig(self, move);
            GravityHelperModule.OverrideSemaphore--;
        }
    }
}
