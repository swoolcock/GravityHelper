// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class HoldableHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Holdable)} hooks...");

            On.Celeste.Holdable.Added += Holdable_Added;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Holdable)} hooks...");

            On.Celeste.Holdable.Added -= Holdable_Added;
        }

        private static void Holdable_Added(On.Celeste.Holdable.orig_Added orig, Holdable self, Entity entity)
        {
            orig(self, entity);

            if (entity.Get<GravityHoldable>() != null) return;
            entity.Add(new GravityHoldable());
        }
    }
}
