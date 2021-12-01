// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class TheoCrystalHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(TheoCrystal)} hooks...");

            On.Celeste.TheoCrystal.Added += TheoCrystal_Added;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(TheoCrystal)} hooks...");

            On.Celeste.TheoCrystal.Added -= TheoCrystal_Added;
        }

        private static void TheoCrystal_Added(On.Celeste.TheoCrystal.orig_Added orig, TheoCrystal self, Scene scene)
        {
            orig(self, scene);
            if (self.Get<GravityComponent>() != null) return;

            var data = new DynData<TheoCrystal>(self);

            self.Add(new GravityComponent
            {
                UpdateVisuals = args =>
                {
                    var sprite = data.Get<Sprite>("sprite");
                    sprite.Scale.Y = args.NewValue == GravityType.Inverted ? -1 : 1;
                },
            });
        }
    }
}
