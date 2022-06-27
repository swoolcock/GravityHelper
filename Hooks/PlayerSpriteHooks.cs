// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class PlayerSpriteHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(PlayerSprite)} hooks...");

            On.Celeste.PlayerSprite.Render += PlayerSprite_Render;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(PlayerSprite)} hooks...");

            On.Celeste.PlayerSprite.Render -= PlayerSprite_Render;
        }

        private static void PlayerSprite_Render(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self)
        {
            if (self.Entity is Player)
            {
                orig(self);
                return;
            }

            var invert = false;
            if (self.Entity is BadelineOldsite || self.Entity.GetType() == ReflectionCache.CelesteNetGhostType)
            {
                invert = self.Entity.ShouldInvert();
            }

            var scaleY = self.Scale.Y;
            if (invert) self.Scale.Y = -scaleY;
            orig(self);
            if (invert) self.Scale.Y = scaleY;
        }
    }
}
