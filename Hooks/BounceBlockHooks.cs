// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    [HookFixture(typeof(BounceBlock))]
    public static class BounceBlockHooks
    {
        // public static void Load()
        // {
        //     Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BounceBlock)} hooks...");
        //
        //     IL.Celeste.BounceBlock.Update += BounceBlock_Update;
        //     IL.Celeste.BounceBlock.WindUpPlayerCheck += BounceBlock_WindUpPlayerCheck;
        // }
        //
        // public static void Unload()
        // {
        //     Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BounceBlock)} hooks...");
        //
        //     IL.Celeste.BounceBlock.Update -= BounceBlock_Update;
        //     IL.Celeste.BounceBlock.WindUpPlayerCheck -= BounceBlock_WindUpPlayerCheck;
        // }

        [ILHook(nameof(BounceBlock.Update))]
        private static void BounceBlock_Update(ILContext il)
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(
                instr => instr.MatchCall<Vector2>("op_UnaryNegation"),
                instr => instr.MatchStfld<BounceBlock>("bounceDir")))
                throw new HookException("Couldn't find bounceDir (1)");
            cursor.Index++;
            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(
                instr => instr.MatchCall<Vector2>("op_UnaryNegation"),
                instr => instr.MatchStfld<BounceBlock>("bounceDir")))
                throw new HookException("Couldn't find bounceDir (2)");
            cursor.Index++;
            cursor.EmitInvertVectorDelegate();
        }

        [ILHook("WindUpPlayerCheck", BindingFlags.Instance | BindingFlags.NonPublic)]
        private static void BounceBlock_WindUpPlayerCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.SubtractionPredicate))
                throw new HookException("Couldn't find subtraction.");

            cursor.EmitInvertVectorDelegate();
        }
    }
}
