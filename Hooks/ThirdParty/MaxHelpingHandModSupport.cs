// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("MaxHelpingHand")]
    public class MaxHelpingHandModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp;
        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove;
        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider;
        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV;
        // ReSharper restore InconsistentNaming

        protected override void Load()
        {
            var mhhudjt = ReflectionCache.MaxHelpingHandUpsideDownJumpThruType;

            var playerMovingUpMethod = mhhudjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
            if (playerMovingUpMethod != null)
            {
                var target = GetType().GetMethod(nameof(MaxHelpingHand_UpsideDownJumpThru_playerMovingUp), BindingFlags.Static | BindingFlags.NonPublic);
                hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp = new Hook(playerMovingUpMethod, target);
            }

            var updateClimbMoveMethod = mhhudjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);
            if (updateClimbMoveMethod != null)
            {
                var target = GetType().GetMethod(nameof(MaxHelpingHand_UpsideDownJumpThru_updateClimbMove), BindingFlags.Static | BindingFlags.NonPublic);
                hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove = new Hook(updateClimbMoveMethod, target);
            }

            var onJumpthruHasPlayerRiderMethod = mhhudjt?.GetMethod("onJumpthruHasPlayerRider", BindingFlags.Static | BindingFlags.NonPublic);
            if (onJumpthruHasPlayerRiderMethod != null)
            {
                hook_MaxHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider = new ILHook(onJumpthruHasPlayerRiderMethod, MaxHelpingHand_onJumpthruHasPlayerRider);
            }

            var onPlayerOnCollideVMethod = mhhudjt?.GetMethod("onPlayerOnCollideV", BindingFlags.Static | BindingFlags.NonPublic);
            if (onPlayerOnCollideVMethod != null)
            {
                hook_MaxHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV = new ILHook(onPlayerOnCollideVMethod, MaxHelpingHand_onPlayerOnCollideV);
            }
        }

        protected override void Unload()
        {
            hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp = null;
            hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove = null;
            hook_MaxHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider = null;
            hook_MaxHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV = null;
        }

        private static int MaxHelpingHand_UpsideDownJumpThru_updateClimbMove(Func<Player, int, int> orig, Player player, int lastClimbMove) =>
            GravityHelperModule.ShouldInvertPlayer ? lastClimbMove : orig(player, lastClimbMove);

        private static bool MaxHelpingHand_UpsideDownJumpThru_playerMovingUp(Func<Player, bool> orig, Player player) =>
            orig(player) != GravityHelperModule.ShouldInvertPlayer;

        private static void MaxHelpingHand_onJumpthruHasPlayerRider(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<On.Celeste.JumpThru.orig_HasPlayerRider, JumpThru, bool>>((orig, self) => orig(self));
            cursor.Emit(OpCodes.Ret);
        });

        private static void MaxHelpingHand_onPlayerOnCollideV(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            var cursor2 = cursor.Clone();
            if (!cursor2.TryGotoNext(
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(1),
                instr => instr.MatchLdarg(2)))
                throw new HookException("Couldn't find orig call");
            cursor.Emit(OpCodes.Br_S, cursor2.Next);
        });
    }
}
