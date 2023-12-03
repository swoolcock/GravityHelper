// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

using Celeste.Mod.GravityHelper.Entities;

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal static class SpringHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Spring)} hooks...");
            On.Celeste.Spring.OnCollide += Spring_OnCollide;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Spring)} hooks...");
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;
        }

        private static void Spring_OnCollide(On.Celeste.Spring.orig_OnCollide orig, Spring self, Player player)
        {
            if (!GravityHelperModule.ShouldInvertPlayer || self.Orientation != Spring.Orientations.Floor)
            {
                orig(self, player);
                return;
            }

            // check copied from orig
            if (player.StateMachine.State == Player.StDreamDash || !self.playerCanUse)
                return;

            // do nothing if moving away
            if (player.Speed.Y > 0)
                return;

            self.BounceAnimate();
            GravitySpring.InvertedSuperBounce(player, self.Top);
        }
    }
}
