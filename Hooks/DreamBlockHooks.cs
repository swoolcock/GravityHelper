// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class DreamBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(DreamBlock)} hooks...");
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(DreamBlock)} hooks...");
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
        {
            orig(self);
            if (self is GravityDreamBlock gravityDreamBlock)
                gravityDreamBlock.UpdateParticleColors();
        }
    }
}
