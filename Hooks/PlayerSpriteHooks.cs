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
            bool invert = false;
            if (self.Entity is Player)
                invert = GravityHelperModule.ShouldInvertPlayer;
            else if (self.Entity is BadelineOldsite baddy)
                invert = baddy.ShouldInvert();

            var scaleY = self.Scale.Y;
            if (invert) self.Scale.Y = -scaleY;
            orig(self);
            if (invert) self.Scale.Y = scaleY;
        }
    }
}
