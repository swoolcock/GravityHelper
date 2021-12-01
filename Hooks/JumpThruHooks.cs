// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.



// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class JumpThruHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.MoveVExact += JumpThru_MoveVExact;
        }

        private static void JumpThru_MoveVExact(On.Celeste.JumpThru.orig_MoveVExact orig, JumpThru self, int move)
        {
            if (!GravityComponent.ShouldInvertPlayer)
            {
                orig(self, move);
                return;
            }

            GravityHelperModule.JumpThruMoving = true;
            orig(self, move);
            GravityHelperModule.JumpThruMoving = false;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(JumpThru)} hooks...");
            On.Celeste.JumpThru.MoveVExact += JumpThru_MoveVExact;
        }
    }
}
