using System;
using System.Reflection;
using Celeste;
using Microsoft.Xna.Framework;
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
        }

        public static void Unload()
        {
            unloadMaxHelpingHand();
        }

        #region MaxHelpingHand

        private static IDetour hook_UpsideDownJumpThru_playerMovingUp;
        private static IDetour hook_UpsideDownJumpThru_updateClimbMove;
        private static IDetour hook_UpsideDownJumpThru_Awake;

        private static void loadMaxHelpingHand()
        {
            var udjt = ReflectionCache.UpsideDownJumpThruType;
            var udjtPlayerMovingUpMethod = udjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtUpdateClimbMoveMethod = udjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtAwakeMethod = udjt?.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);

            if (udjtPlayerMovingUpMethod != null)
                hook_UpsideDownJumpThru_playerMovingUp = new ILHook(udjtPlayerMovingUpMethod, UpsideDownJumpThru_playerMovingUp);

            if (udjtUpdateClimbMoveMethod != null)
                hook_UpsideDownJumpThru_updateClimbMove = new ILHook(udjtUpdateClimbMoveMethod, UpsideDownJumpThru_updateClimbMove);

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

            hook_UpsideDownJumpThru_Awake?.Dispose();
            hook_UpsideDownJumpThru_Awake = null;
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

        #endregion
    }
}