// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BounceBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BounceBlock)} hooks...");

            IL.Celeste.BounceBlock.WindUpPlayerCheck += BounceBlock_WindUpPlayerCheck;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BounceBlock)} hooks...");

            IL.Celeste.BounceBlock.WindUpPlayerCheck -= BounceBlock_WindUpPlayerCheck;
        }

        private static void BounceBlock_WindUpPlayerCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.SubtractionPredicate))
                throw new HookException("Couldn't find subtraction.");

            cursor.EmitInvertVectorDelegate();
        });
    }
}
