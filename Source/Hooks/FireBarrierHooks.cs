// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class FireBarrierHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(FireBarrier)} hooks...");
        On.Celeste.FireBarrier.Added += FireBarrier_Added;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(FireBarrier)} hooks...");
        On.Celeste.FireBarrier.Added -= FireBarrier_Added;
    }

    private static void FireBarrier_Added(On.Celeste.FireBarrier.orig_Added orig, FireBarrier self, Scene scene)
    {
        orig(self, scene);

        var solid = self.solid;
        var initialY = solid.Y;

        self.Add(new PlayerGravityListener((player, args) =>
            solid.Y = args.NewValue == GravityType.Inverted ? initialY - 1 : initialY));
    }
}