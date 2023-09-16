// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class FloatySpaceBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(FloatySpaceBlock)} hooks...");
            IL.Celeste.FloatySpaceBlock.Update += FloatySpaceBlock_Update;
            IL.Celeste.FloatySpaceBlock.MoveToTarget += FloatySpaceBlock_MoveToTarget;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(FloatySpaceBlock)} hooks...");
            IL.Celeste.FloatySpaceBlock.Update -= FloatySpaceBlock_Update;
            IL.Celeste.FloatySpaceBlock.MoveToTarget -= FloatySpaceBlock_MoveToTarget;
        }

        private static void FloatySpaceBlock_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<FloatySpaceBlock>("yLerp"),
                instr => instr.MatchLdcR4(1)))
                throw new HookException("Couldn't invert Calc.Approach");

            cursor.EmitInvertFloatDelegate();
        });

        private static void FloatySpaceBlock_MoveToTarget(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(12f)))
                throw new HookException("Couldn't find 12f");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, FloatySpaceBlock, float>>((f, self) =>
            {
                var yLerp = self.yLerp;
                return f * Math.Sign(yLerp);
            });
        });
    }
}
