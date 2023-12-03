// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal static class BadelineDummyHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BadelineDummy)} hooks...");

            On.Celeste.BadelineDummy.Render += BadelineDummy_Render;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BadelineDummy)} hooks...");

            On.Celeste.BadelineDummy.Render -= BadelineDummy_Render;
        }

        private static void BadelineDummy_Render(On.Celeste.BadelineDummy.orig_Render orig, BadelineDummy self)
        {
            if (!self.ShouldInvert())
            {
                orig(self);
                return;
            }

            var scale = self.Sprite.Scale;
            self.Sprite.Scale.Y = -scale.Y;
            orig(self);
            self.Sprite.Scale.Y = scale.Y;
        }
    }
}
