// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class FlyFeatherHooks
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_FlyFeather_CollectRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(FlyFeather)} hooks...");
            hook_FlyFeather_CollectRoutine = new ILHook(ReflectionCache.FlyFeather_CollectRoutine.GetStateMachineTarget(), FlyFeather_CollectRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(FlyFeather)} hooks...");
            hook_FlyFeather_CollectRoutine?.Dispose();
            hook_FlyFeather_CollectRoutine = null;
        }

        private static void FlyFeather_CollectRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchCall(typeof(SlashFx), nameof(SlashFx.Burst))))
                throw new HookException("Couldn't find SlashFx.Burst");

            cursor.EmitInvertFloatDelegate();
        });
    }
}
