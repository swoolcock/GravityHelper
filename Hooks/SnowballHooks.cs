// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Monocle;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SnowballHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Snowball)} hooks...");

            On.Celeste.Snowball.Update += Snowball_Update;
        }


        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Snowball)} hooks...");

            On.Celeste.Snowball.Update -= Snowball_Update;
        }

        private static void Snowball_Update(On.Celeste.Snowball.orig_Update orig, Snowball self)
        {
            var data = DynamicData.For(self);
            var bounceCollider = (Hitbox)data.Get<Collider>("bounceCollider");
            var collider = self.Collider;

            if (GravityHelperModule.ShouldInvertPlayer != bounceCollider.Top > collider.Top)
            {
                bounceCollider.Top = -bounceCollider.Bottom;
                collider.Top = -collider.Bottom;
            }

            orig(self);
        }
    }
}
