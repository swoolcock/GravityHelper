// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("FancyTileEntities")]
    public static class FancyTileEntitiesModSupport
    {
        private const string fancy_falling_block_type = "Celeste.Mod.FancyTileEntities.FancyFallingBlock";
        [ReflectType("FancyTileEntities", fancy_falling_block_type)]
        public static Type FancyFallingBlockType;

        private static MethodInfo _surfaceSoundIndexAt;
        public static MethodInfo FancyFallingBlockSurfaceSoundIndexAt => _surfaceSoundIndexAt ??= FancyFallingBlockType?.GetMethod("SurfaceSoundIndexAt", BindingFlags.Instance | BindingFlags.NonPublic);

        private static int callFancyFallingBlockSurfaceSoundIndexAt(this FallingBlock fallingBlock, Vector2 readPosition)
        {
            if (FancyFallingBlockSurfaceSoundIndexAt == null) return -1;
            return (int) FancyFallingBlockSurfaceSoundIndexAt.Invoke(fallingBlock, new object[] {readPosition});
        }

        [HookMethod(fancy_falling_block_type, "MoveVExact")]
        private static void FancyFallingBlock_MoveVExact(Action<FallingBlock, int> orig, FallingBlock self, int move)
        {
            GravityHelperModule.OverrideSemaphore++;
            orig(self, move);
            GravityHelperModule.OverrideSemaphore--;
        }

        [HookMethod(fancy_falling_block_type, "GetLandSoundIndex")]
        private static int FancyFallingBlock_GetLandSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, entity);

            int num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        [HookMethod(fancy_falling_block_type, "GetStepSoundIndex")]
        private static int FancyFallingBlock_GetStepSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, entity);

            int num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.callFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }
    }
}
