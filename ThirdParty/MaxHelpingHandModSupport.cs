// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("MaxHelpingHand")]
    public static class MaxHelpingHandModSupport
    {
        private const string upside_down_jumpthru_type = "Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru";
        [ReflectType("MaxHelpingHand", upside_down_jumpthru_type)]
        public static Type MaxHelpingHandUpsideDownJumpThruType;

        private const string grouped_trigger_spikes_type = "Celeste.Mod.MaxHelpingHand.Entities.GroupedTriggerSpikes";
        [ReflectType("MaxHelpingHand", grouped_trigger_spikes_type)]
        public static Type MaxHelpingHandGroupedTriggerSpikesType;

        [HookMethod(upside_down_jumpthru_type, "updateClimbMove")]
        private static int MaxHelpingHand_UpsideDownJumpThru_updateClimbMove(Func<Player, int, int> orig, Player player, int lastClimbMove) =>
            GravityHelperModule.ShouldInvertPlayer ? lastClimbMove : orig(player, lastClimbMove);

        [HookMethod(upside_down_jumpthru_type, "playerMovingUp")]
        private static bool MaxHelpingHand_UpsideDownJumpThru_playerMovingUp(Func<Player, bool> orig, Player player) =>
            orig(player) != GravityHelperModule.ShouldInvertPlayer;

        [HookMethod(upside_down_jumpthru_type, "onJumpthruHasPlayerRider")]
        private static void MaxHelpingHand_onJumpthruHasPlayerRider(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<On.Celeste.JumpThru.orig_HasPlayerRider, JumpThru, bool>>((orig, self) => orig(self));
            cursor.Emit(OpCodes.Ret);
        });

        [HookMethod(upside_down_jumpthru_type, "onPlayerOnCollideV")]
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

        [HookMethod(grouped_trigger_spikes_type, "GetPlayerCollideIndex")]
        private static void GroupedTriggerSpikes_GetPlayerCollideIndex(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find first Speed.Y");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find second Speed.Y");
            cursor.EmitInvertFloatDelegate();
        });

        [HookMethod(grouped_trigger_spikes_type, "ctor", 7)]
        private static void GroupedTriggerSpikes_ctor(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchRet()))
                throw new HookException("Couldn't find return");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(self =>
            {
                var data = new DynamicData(self);
                var direction = data.Get<Spikes.Directions>("direction");
                var method = MaxHelpingHandGroupedTriggerSpikesType.GetMethod("UpSafeBlockCheck", BindingFlags.NonPublic | BindingFlags.Instance);

                // we add a disabled ledge blocker for downward spikes
                if (direction == Spikes.Directions.Down)
                {
                    self.Add(new SafeGroundBlocker());
                    self.Add(new LedgeBlocker
                    {
                        Blocking = false,
                        BlockChecker = player => (bool)method.Invoke(self, new object[] { player }),
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
}
