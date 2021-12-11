// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BoosterHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Booster)} hooks...");
            On.Celeste.Booster.PlayerReleased += Booster_PlayerReleased;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Booster)} hooks...");
            On.Celeste.Booster.PlayerReleased -= Booster_PlayerReleased;
        }

        private static void Booster_PlayerReleased(On.Celeste.Booster.orig_PlayerReleased orig, Booster self)
        {
            orig(self);

            if (self is GravityBooster gravityBooster)
                GravityHelperModule.PlayerComponent.SetGravity(gravityBooster.GravityType);
        }
    }
}
