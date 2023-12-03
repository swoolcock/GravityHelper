// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("FancyTileEntities")]
    internal class FancyTileEntitiesModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_FancyFallingBlock_MoveVExact;
        private static IDetour hook_FancyFallingBlock_GetLandSoundIndex;
        private static IDetour hook_FancyFallingBlock_GetStepSoundIndex;
        // ReSharper restore InconsistentNaming

        protected override void Load()
        {
            var ffbt = ReflectionCache.FancyFallingBlockType;
            var ffbtMoveVExactMethod = ffbt?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetLandSoundIndexMethod = ffbt?.GetMethod("GetLandSoundIndex", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetStepSoundIndexMethod = ffbt?.GetMethod("GetStepSoundIndex", BindingFlags.Instance | BindingFlags.Public);

            if (ffbtMoveVExactMethod != null)
            {
                var target = GetType().GetMethod(nameof(FancyFallingBlock_MoveVExact), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_MoveVExact = new Hook(ffbtMoveVExactMethod, target);
            }

            if (ffbtGetLandSoundIndexMethod != null)
            {
                var target = GetType().GetMethod(nameof(FancyFallingBlock_GetLandSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetLandSoundIndex = new Hook(ffbtGetLandSoundIndexMethod, target);
            }

            if (ffbtGetStepSoundIndexMethod != null)
            {
                var target = GetType().GetMethod(nameof(FancyFallingBlock_GetStepSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetStepSoundIndex = new Hook(ffbtGetStepSoundIndexMethod, target);
            }
        }

        protected override void Unload()
        {
            hook_FancyFallingBlock_MoveVExact?.Dispose();
            hook_FancyFallingBlock_MoveVExact = null;

            hook_FancyFallingBlock_GetLandSoundIndex?.Dispose();
            hook_FancyFallingBlock_GetLandSoundIndex = null;

            hook_FancyFallingBlock_GetStepSoundIndex?.Dispose();
            hook_FancyFallingBlock_GetStepSoundIndex = null;
        }

        private static void FancyFallingBlock_MoveVExact(Action<FallingBlock, int> orig, FallingBlock self, int move)
        {
            GravityHelperModule.OverrideSemaphore++;
            orig(self, move);
            GravityHelperModule.OverrideSemaphore--;
        }

        private static int FancyFallingBlock_GetLandSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static int FancyFallingBlock_GetStepSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }
    }
}
