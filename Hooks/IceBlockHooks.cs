// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class IceBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(IceBlock)} hooks...");
            On.Celeste.IceBlock.Added += IceBlock_Added;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(IceBlock)} hooks...");
            On.Celeste.IceBlock.Added -= IceBlock_Added;
        }

        private static void IceBlock_Added(On.Celeste.IceBlock.orig_Added orig, IceBlock self, Scene scene)
        {
            orig(self, scene);

            var solid = self.solid;
            var initialY = solid.Y;

            self.Add(new PlayerGravityListener((player, args) =>
                solid.Y = args.NewValue == GravityType.Inverted ? initialY - 1 : initialY));
        }
    }
}
