// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.ThirdParty;

[ThirdPartyMod("MaxHelpingHand")]
internal class MaddieHelpingHandModSupport : ThirdPartyModSupport
{
    // ReSharper disable InconsistentNaming
    private static IDetour hook_MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp;
    private static IDetour hook_MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove;
    private static IDetour hook_MaddieHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider;
    private static IDetour hook_MaddieHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV;
    private static IDetour hook_MaddieHelpingHand_GroupedTriggerSpikes_GetPlayerCollideIndex;
    private static IDetour hook_MaddieHelpingHand_GroupedTriggerSpikes_ctor;
    // ReSharper restore InconsistentNaming

    protected override void Load(GravityHelperModule.HookLevel hookLevel)
    {
        var mhhudjt = ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType;

        var playerMovingUpMethod = mhhudjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
        if (playerMovingUpMethod != null)
        {
            var target = GetType().GetMethod(nameof(MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp), BindingFlags.Static | BindingFlags.NonPublic);
            hook_MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp = new Hook(playerMovingUpMethod, target);
        }

        var updateClimbMoveMethod = mhhudjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);
        if (updateClimbMoveMethod != null)
        {
            var target = GetType().GetMethod(nameof(MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove), BindingFlags.Static | BindingFlags.NonPublic);
            hook_MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove = new Hook(updateClimbMoveMethod, target);
        }

        var onJumpthruHasPlayerRiderMethod = mhhudjt?.GetMethod("onJumpthruHasPlayerRider", BindingFlags.Static | BindingFlags.NonPublic);
        if (onJumpthruHasPlayerRiderMethod != null)
        {
            hook_MaddieHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider = new ILHook(onJumpthruHasPlayerRiderMethod, MaddieHelpingHand_onJumpthruHasPlayerRider);
        }

        // we only apply corner correction if it's actually a gravity helper map, and it's enabled in options
        if (hookLevel is GravityHelperModule.HookLevel.GravityHelperMap && GravityHelperModule.Settings.MHHUDJTCornerCorrection)
        {
            var onPlayerOnCollideVMethod = mhhudjt?.GetMethod("onPlayerOnCollideV", BindingFlags.Static | BindingFlags.NonPublic);
            if (onPlayerOnCollideVMethod != null)
            {
                hook_MaddieHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV = new ILHook(onPlayerOnCollideVMethod, MaddieHelpingHand_onPlayerOnCollideV);
            }
        }

        var mhhgtst = ReflectionCache.MaddieHelpingHandGroupedTriggerSpikesType;
        var getPlayerCollideIndexMethod = mhhgtst?.GetMethod("GetPlayerCollideIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        if (getPlayerCollideIndexMethod != null)
        {
            hook_MaddieHelpingHand_GroupedTriggerSpikes_GetPlayerCollideIndex = new ILHook(getPlayerCollideIndexMethod, GroupedTriggerSpikes_GetPlayerCollideIndex);
        }

        var ctor = mhhgtst?.GetConstructors().FirstOrDefault(c => c.GetParameters().Length > 3);
        if (ctor != null)
        {
            hook_MaddieHelpingHand_GroupedTriggerSpikes_ctor = new ILHook(ctor, GroupedTriggerSpikes_ctor);
        }
    }

    protected override void Unload()
    {
        hook_MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp?.Dispose();
        hook_MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp = null;
        hook_MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove?.Dispose();
        hook_MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove = null;
        hook_MaddieHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider?.Dispose();
        hook_MaddieHelpingHand_UpsideDownJumpThru_onJumpthruHasPlayerRider = null;
        hook_MaddieHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV?.Dispose();
        hook_MaddieHelpingHand_UpsideDownJumpThru_onPlayerOnCollideV = null;
        hook_MaddieHelpingHand_GroupedTriggerSpikes_GetPlayerCollideIndex?.Dispose();
        hook_MaddieHelpingHand_GroupedTriggerSpikes_GetPlayerCollideIndex = null;
        hook_MaddieHelpingHand_GroupedTriggerSpikes_ctor?.Dispose();
        hook_MaddieHelpingHand_GroupedTriggerSpikes_ctor = null;
    }

    private static int MaddieHelpingHand_UpsideDownJumpThru_updateClimbMove(Func<Player, int, int> orig, Player player, int lastClimbMove) =>
        GravityHelperModule.ShouldInvertPlayer ? lastClimbMove : orig(player, lastClimbMove);

    private static bool MaddieHelpingHand_UpsideDownJumpThru_playerMovingUp(Func<Player, bool> orig, Player player) =>
        orig(player) != GravityHelperModule.ShouldInvertPlayer;

    private static void MaddieHelpingHand_onJumpthruHasPlayerRider(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<On.Celeste.JumpThru.orig_HasPlayerRider, JumpThru, bool>>((orig, self) => orig(self));
        cursor.Emit(OpCodes.Ret);
    });

    private static void MaddieHelpingHand_onPlayerOnCollideV(ILContext il) => HookUtils.SafeHook(() =>
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

    private void GroupedTriggerSpikes_GetPlayerCollideIndex(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
            throw new HookException("Couldn't find first Speed.Y");
        cursor.EmitInvertFloatDelegate();

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
            throw new HookException("Couldn't find second Speed.Y");
        cursor.EmitInvertFloatDelegate();
    });

    private void GroupedTriggerSpikes_ctor(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(instr => instr.MatchRet()))
            throw new HookException("Couldn't find return");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Entity>>(self =>
        {
            var data = new DynamicData(self);
            var direction = data.Get<Spikes.Directions>("direction");
            var method = ReflectionCache.MaddieHelpingHandGroupedTriggerSpikesType.GetMethod("UpSafeBlockCheck", BindingFlags.NonPublic | BindingFlags.Instance);

            // we add a disabled ledge blocker for downward spikes
            if (direction == Spikes.Directions.Down)
            {
                self.Add(new SafeGroundBlocker());
                self.Add(new LedgeBlocker
                {
                    Blocking = false,
                    BlockChecker = player => (bool)method.Invoke(self, [player]),
                });
            }

            // add a player listener to toggle the ledge blockers
            if (direction == Spikes.Directions.Down || direction == Spikes.Directions.Up)
            {
                self.Add(new PlayerGravityListener
                {
                    GravityChanged = (_, args) =>
                    {
                        var ledgeBlocker = self.Components.Get<LedgeBlocker>();
                        var safeGroundBlocker = self.Components.Get<SafeGroundBlocker>();
                        if (direction == Spikes.Directions.Up)
                            safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Normal;
                        else if (direction == Spikes.Directions.Down)
                            safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Inverted;
                    },
                });
            }
        });
    });
}
