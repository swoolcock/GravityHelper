// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SolidTilesHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(SolidTiles)} hooks...");

            On.Celeste.SolidTiles.GetLandSoundIndex += SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex += SolidTiles_GetStepSoundIndex;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(SolidTiles)} hooks...");

            On.Celeste.SolidTiles.GetLandSoundIndex -= SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex -= SolidTiles_GetStepSoundIndex;
        }

        private static int SolidTiles_GetLandSoundIndex(On.Celeste.SolidTiles.orig_GetLandSoundIndex orig, SolidTiles self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static int SolidTiles_GetStepSoundIndex(On.Celeste.SolidTiles.orig_GetStepSoundIndex orig, SolidTiles self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }
    }
}
