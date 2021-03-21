using System.Reflection;
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

        private static void loadMaxHelpingHand()
        {
            var udjt = ReflectionCache.UpsideDownJumpThruType;
            var udjtPlayerMovingUpMethod = udjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
            var udjtUpdateClimbMoveMethod = udjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);

            if (udjtPlayerMovingUpMethod != null)
                hook_UpsideDownJumpThru_playerMovingUp = new ILHook(udjtPlayerMovingUpMethod, UpsideDownJumpThru_playerMovingUp);

            if (udjtUpdateClimbMoveMethod != null)
                hook_UpsideDownJumpThru_updateClimbMove = new ILHook(udjtUpdateClimbMoveMethod, UpsideDownJumpThru_updateClimbMove);
        }

        private static void unloadMaxHelpingHand()
        {
            hook_UpsideDownJumpThru_playerMovingUp?.Dispose();
            hook_UpsideDownJumpThru_playerMovingUp = null;

            hook_UpsideDownJumpThru_updateClimbMove?.Dispose();
            hook_UpsideDownJumpThru_updateClimbMove = null;
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

        #endregion
    }
}