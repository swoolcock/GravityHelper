// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class StarJumpBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(StarJumpBlock)} hooks...");
            IL.Celeste.StarJumpBlock.Update += StarJumpBlock_Update;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(StarJumpBlock)} hooks...");
            IL.Celeste.StarJumpBlock.Update -= StarJumpBlock_Update;
        }

        private static void StarJumpBlock_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<StarJumpBlock>("yLerp"),
                instr => instr.MatchLdcR4(1)))
                throw new HookException("Couldn't invert Calc.Approach");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(12f)))
                throw new HookException("Couldn't find 12f");

            var yLerpField = typeof(StarJumpBlock).GetField("yLerp", BindingFlags.Instance | BindingFlags.NonPublic);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, StarJumpBlock, float>>((f, self) =>
            {
                var yLerp = (float)yLerpField.GetValue(self);
                return f * Math.Sign(yLerp);
            });
        });
    }
}
