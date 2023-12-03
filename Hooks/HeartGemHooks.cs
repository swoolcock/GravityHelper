// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal static class HeartGemHooks
    {
        private static IDetour hook_HeartGem_orig_CollectRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(HeartGem)} hooks...");
            var collectRoutineMethod = typeof(HeartGem).GetMethod("orig_CollectRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
            hook_HeartGem_orig_CollectRoutine = new ILHook(collectRoutineMethod, HeartGem_orig_CollectRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(HeartGem)} hooks...");
            hook_HeartGem_orig_CollectRoutine?.Dispose();
            hook_HeartGem_orig_CollectRoutine = null;
        }

        private static void HeartGem_orig_CollectRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            for (int i = 0; i < 3; i++)
            {
                if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj<InvisibleBarrier>()))
                    throw new HookException($"Couldn't find new InvisibleBarrier ({i})");
            }

            // move the top invisible barrier to the bottom if we're inverted
            cursor.EmitDelegate<Func<InvisibleBarrier, InvisibleBarrier>>(barrier =>
            {
                if (GravityHelperModule.ShouldInvertPlayer && Engine.Scene is Level level)
                    barrier.Position.Y += level.Bounds.Height;

                return barrier;
            });
        });
    }
}
