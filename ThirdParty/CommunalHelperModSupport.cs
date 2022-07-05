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

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("CommunalHelper")]
    public class CommunalHelperModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_CommunalHelper_ConnectedSolid_MoveVExact;
        private static IDetour hook_CommunalHelper_TimedTriggerSpikes_OnCollide;
        private static IDetour hook_CommunalHelper_TimedTriggerSpikes_ctor;
        // ReSharper restore InconsistentNaming

        protected override void Load()
        {
            var chcst = ReflectionCache.CommunalHelperConnectedSolidType;
            var moveVExactMethod = chcst?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            if (moveVExactMethod != null)
            {
                var target = GetType().GetMethod(nameof(ConnectedSolid_MoveVExact), BindingFlags.Static | BindingFlags.NonPublic);
                hook_CommunalHelper_ConnectedSolid_MoveVExact = new Hook(moveVExactMethod, target);
            }

            var chttst = ReflectionCache.CommunalHelperTimedTriggerSpikesType;
            var onCollideMethod = chttst?.GetMethod("OnCollide", BindingFlags.Instance | BindingFlags.NonPublic);
            if (onCollideMethod != null)
            {
                hook_CommunalHelper_TimedTriggerSpikes_OnCollide = new ILHook(onCollideMethod, TimedTriggerSpikes_OnCollide);
            }

            var ctor = chttst?.GetConstructors().FirstOrDefault(c => c.GetParameters().Length > 3);
            if (ctor != null)
            {
                hook_CommunalHelper_TimedTriggerSpikes_ctor = new ILHook(ctor, TimedTriggerSpikes_ctor);
            }
        }


        protected override void Unload()
        {
            hook_CommunalHelper_ConnectedSolid_MoveVExact?.Dispose();
            hook_CommunalHelper_ConnectedSolid_MoveVExact = null;
            hook_CommunalHelper_TimedTriggerSpikes_OnCollide?.Dispose();
            hook_CommunalHelper_TimedTriggerSpikes_OnCollide = null;
            hook_CommunalHelper_TimedTriggerSpikes_ctor?.Dispose();
            hook_CommunalHelper_TimedTriggerSpikes_ctor = null;
        }

        private static void ConnectedSolid_MoveVExact(Action<Solid, int> orig, Solid self, int move)
        {
            GravityHelperModule.OverrideSemaphore++;
            orig(self, move);
            GravityHelperModule.OverrideSemaphore--;
        }

        private void TimedTriggerSpikes_OnCollide(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find first Speed.Y");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find second Speed.Y");
            cursor.EmitInvertFloatDelegate();
        });

        private void TimedTriggerSpikes_ctor(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchRet()))
                throw new HookException("Couldn't find return");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Entity>>(self =>
            {
                var data = new DynamicData(self);
                var direction = data.Get<Spikes.Directions>("direction");
                var method = ReflectionCache.CommunalHelperTimedTriggerSpikesType.GetMethod("UpSafeBlockCheck", BindingFlags.NonPublic | BindingFlags.Instance);

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
