using System;
using System.Reflection;
using Celeste;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace GravityHelper
{
    public static class ThirdPartyHooks
    {
        public static void Load()
        {
            loadMaxHelpingHand();
            loadFancyTileEntities();
        }

        public static void Unload()
        {
            unloadMaxHelpingHand();
            unloadFancyTileEntities();
        }

        #region MaxHelpingHand

        private static IDetour hook_UpsideDownJumpThru_playerMovingUp;
        private static IDetour hook_UpsideDownJumpThru_updateClimbMove;
        private static IDetour hook_UpsideDownJumpThru_onJumpthruHasPlayerRider;
        private static IDetour hook_UpsideDownJumpThru_MoveVExact;
        private static IDetour hook_UpsideDownJumpThru_Awake;

        private static void loadMaxHelpingHand()
        {
            var udjt = ReflectionCache.UpsideDownJumpThruType;
            var udjtPlayerMovingUpMethod = udjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtUpdateClimbMoveMethod = udjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtOnJumpthruHasPlayerRiderMethod = udjt?.GetMethod("onJumpthruHasPlayerRider", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtMoveVExactMethod = udjt?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            var udjtAwakeMethod = udjt?.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);

            if (udjtPlayerMovingUpMethod != null)
                hook_UpsideDownJumpThru_playerMovingUp = new ILHook(udjtPlayerMovingUpMethod, UpsideDownJumpThru_playerMovingUp);

            if (udjtUpdateClimbMoveMethod != null)
                hook_UpsideDownJumpThru_updateClimbMove = new ILHook(udjtUpdateClimbMoveMethod, UpsideDownJumpThru_updateClimbMove);

            if (udjtOnJumpthruHasPlayerRiderMethod != null)
                hook_UpsideDownJumpThru_onJumpthruHasPlayerRider = new ILHook(udjtOnJumpthruHasPlayerRiderMethod, UpsideDownJumpThru_onJumpthruHasPlayerRider);

            if (udjtMoveVExactMethod != null)
                hook_UpsideDownJumpThru_MoveVExact = new ILHook(udjtMoveVExactMethod, UpsideDownJumpThru_MoveVExact);

            if (udjtAwakeMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(UpsideDownJumpThru_Awake), BindingFlags.Static | BindingFlags.NonPublic);
                hook_UpsideDownJumpThru_Awake = new Hook(udjtAwakeMethod, target);
            }
        }

        private static void unloadMaxHelpingHand()
        {
            hook_UpsideDownJumpThru_playerMovingUp?.Dispose();
            hook_UpsideDownJumpThru_playerMovingUp = null;

            hook_UpsideDownJumpThru_updateClimbMove?.Dispose();
            hook_UpsideDownJumpThru_updateClimbMove = null;

            hook_UpsideDownJumpThru_onJumpthruHasPlayerRider?.Dispose();
            hook_UpsideDownJumpThru_onJumpthruHasPlayerRider = null;

            hook_UpsideDownJumpThru_MoveVExact?.Dispose();
            hook_UpsideDownJumpThru_MoveVExact = null;

            hook_UpsideDownJumpThru_Awake?.Dispose();
            hook_UpsideDownJumpThru_Awake = null;
        }

        private static void UpsideDownJumpThru_onJumpthruHasPlayerRider(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdarg(0));
            var target = cursor.Next;
            cursor.GotoPrev(instr => instr.MatchLdarg(1));
            cursor.EmitDelegate<Func<bool>>(() => GravityHelperModule.ShouldInvert);
            cursor.Emit(OpCodes.Brtrue_S, target);
        }

        private static void UpsideDownJumpThru_playerMovingUp(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
        }

        private static void UpsideDownJumpThru_updateClimbMove(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<VirtualIntegerAxis>(nameof(VirtualIntegerAxis.Value)));
            cursor.EmitInvertIntDelegate();
        }

        private static void UpsideDownJumpThru_Awake(Action<JumpThru, Scene> orig, JumpThru self, Scene scene)
        {
            orig(self, scene);

            var overrideTexture = ReflectionCache.UpsideDownJumpThru_OverrideTexture.GetValue(self) as string ?? "default";

            self.SurfaceSoundIndex = overrideTexture.ToLower() switch
            {
                "dream" => SurfaceIndex.AuroraGlass,
                "temple" => SurfaceIndex.Brick,
                "templeb" => SurfaceIndex.Brick,
                "core" => SurfaceIndex.Dirt,
                _ => SurfaceIndex.Wood,
            };
        }

        private static void UpsideDownJumpThru_MoveVExact(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<JumpThru>(nameof(JumpThru.MoveVExact)));
            cursor.Index -= 2;
            var target = cursor.Next;
            cursor.Index = 0;
            cursor.EmitDelegate<Func<bool>>(() => GravityHelperModule.ShouldInvert);
            cursor.Emit(OpCodes.Brtrue_S, target);
        }

        #endregion

        #region FancyTileEntities

        private static IDetour hook_FancyFallingBlock_MoveVExact;
        private static IDetour hook_FancyFallingBlock_GetLandSoundIndex;
        private static IDetour hook_FancyFallingBlock_GetStepSoundIndex;

        private static void loadFancyTileEntities()
        {
            var ffbt = ReflectionCache.FancyFallingBlockType;
            var ffbtMoveVExactMethod = ffbt?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetLandSoundIndexMethod = ffbt?.GetMethod("GetLandSoundIndex", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetStepSoundIndexMethod = ffbt?.GetMethod("GetStepSoundIndex", BindingFlags.Instance | BindingFlags.Public);

            if (ffbtMoveVExactMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_MoveVExact), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_MoveVExact = new Hook(ffbtMoveVExactMethod, target);
            }

            if (ffbtGetLandSoundIndexMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_GetLandSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetLandSoundIndex = new Hook(ffbtGetLandSoundIndexMethod, target);
            }

            if (ffbtGetStepSoundIndexMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_GetStepSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetStepSoundIndex = new Hook(ffbtGetStepSoundIndexMethod, target);
            }
        }

        private static void unloadFancyTileEntities()
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
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, move);
                return;
            }

            GravityHelperModule.SolidMoving = true;
            orig(self, move);
            GravityHelperModule.SolidMoving = false;
        }

        private static int FancyFallingBlock_GetLandSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static int FancyFallingBlock_GetStepSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        #endregion
    }
}