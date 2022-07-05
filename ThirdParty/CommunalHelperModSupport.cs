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
    [HookFixture("CommunalHelper")]
    public static class CommunalHelperModSupport
    {
        private const string connected_solid_type = "Celeste.Mod.CommunalHelper.ConnectedSolid";
        private const string timed_trigger_spikes_type = "Celeste.Mod.CommunalHelper.Entities.TimedTriggerSpikes";

        [ReflectType("CommunalHelper", timed_trigger_spikes_type)]
        public static Type TimedTriggerSpikesType;

        [HookMethod(connected_solid_type, "MoveVExact")]
        private static void ConnectedSolid_MoveVExact(Action<Solid, int> orig, Solid self, int move)
        {
            GravityHelperModule.OverrideSemaphore++;
            orig(self, move);
            GravityHelperModule.OverrideSemaphore--;
        }

        [HookMethod(timed_trigger_spikes_type, "OnCollide")]
        private static void TimedTriggerSpikes_OnCollide(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find first Speed.Y");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find second Speed.Y");
            cursor.EmitInvertFloatDelegate();
        });

        [HookMethod(timed_trigger_spikes_type, "ctor")]
        private static void TimedTriggerSpikes_ctor(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchRet()))
                throw new HookException("Couldn't find return");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(self =>
            {
                var data = new DynamicData(self);
                var direction = data.Get<Spikes.Directions>("direction");
                var method = TimedTriggerSpikesType.GetMethod("UpSafeBlockCheck", BindingFlags.NonPublic | BindingFlags.Instance);

                // we add a disabled ledge blocker for downward spikes
                // note that there's a bug with timed trigger spikes
                // where the ledge blocker does not correctly handle the right-hand side
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
