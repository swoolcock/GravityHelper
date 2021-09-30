// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class JumpThruHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.HasPlayerRider += JumpThru_HasPlayerRider;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.HasPlayerRider -= JumpThru_HasPlayerRider;
        }

        private static bool JumpThru_HasPlayerRider(On.Celeste.JumpThru.orig_HasPlayerRider orig, JumpThru self) =>
            GravityHelperModule.ShouldInvert == self is UpsideDownJumpThru && orig(self);
    }
}
