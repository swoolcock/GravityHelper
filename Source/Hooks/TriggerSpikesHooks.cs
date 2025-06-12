// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// #define TRIGGER_SPIKES_ORIGINAL

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class TriggerSpikesHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(TriggerSpikes)} hooks...");

        On.Celeste.TriggerSpikes.IsRiding_JumpThru += TriggerSpikes_IsRiding_JumpThru;
        On.Celeste.TriggerSpikes.ctor_Vector2_int_Directions += TriggerSpikes_ctor_Vector2_int_Directions;
        On.Celeste.TriggerSpikes.GetPlayerCollideIndex += TriggerSpikes_GetPlayerCollideIndex;

#if TRIGGER_SPIKES_ORIGINAL
        LoadTriggerSpikesOriginal();
#endif
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(TriggerSpikes)} hooks...");

        On.Celeste.TriggerSpikes.IsRiding_JumpThru -= TriggerSpikes_IsRiding_JumpThru;
        On.Celeste.TriggerSpikes.ctor_Vector2_int_Directions -= TriggerSpikes_ctor_Vector2_int_Directions;
        On.Celeste.TriggerSpikes.GetPlayerCollideIndex -= TriggerSpikes_GetPlayerCollideIndex;

#if TRIGGER_SPIKES_ORIGINAL
        UnloadTriggerSpikesOriginal();
#endif
    }

    private static bool TriggerSpikes_IsRiding_JumpThru(On.Celeste.TriggerSpikes.orig_IsRiding_JumpThru orig, TriggerSpikes self, JumpThru jumpThru)
    {
        // accept regular logic
        if (orig(self, jumpThru))
            return true;
        // also allow for down spikes to attach to gravity helper upside down jumpthrus (not maddie's)
        if (self.direction == TriggerSpikes.Directions.Down && jumpThru is UpsideDownJumpThru && self.CollideCheck(jumpThru, self.Position - Vector2.UnitY))
            return true;
        // otherwise not riding
        return false;
    }

    private static void TriggerSpikes_ctor_Vector2_int_Directions(On.Celeste.TriggerSpikes.orig_ctor_Vector2_int_Directions orig, TriggerSpikes self, Vector2 position, int size, TriggerSpikes.Directions direction)
    {
        orig(self, position, size, direction);

        // we add a disabled ledge blocker for downward spikes
        if (self.direction == TriggerSpikes.Directions.Down)
        {
            self.Add(new SafeGroundBlocker());
            self.Add(new LedgeBlocker(self.UpSafeBlockCheck));
        }

        if (self.direction == TriggerSpikes.Directions.Down || self.direction == TriggerSpikes.Directions.Up)
        {
            self.Add(new PlayerGravityListener
            {
                GravityChanged = (_, args) =>
                {
                    var ledgeBlocker = self.Get<LedgeBlocker>();
                    var safeGroundBlocker = self.Get<SafeGroundBlocker>();

                    if (self.direction == TriggerSpikes.Directions.Up)
                        safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Normal;
                    else if (self.direction == TriggerSpikes.Directions.Down)
                        safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Inverted;
                },
            });
        }
    }

    private static void TriggerSpikes_GetPlayerCollideIndex(On.Celeste.TriggerSpikes.orig_GetPlayerCollideIndex orig, TriggerSpikes self, Player player, out int minIndex, out int maxIndex)
    {
        // left and right spikes just behave as usual, as does non-inverted
        if (self.direction == TriggerSpikes.Directions.Left ||
            self.direction == TriggerSpikes.Directions.Right ||
            !GravityHelperModule.ShouldInvertPlayer)
        {
            orig(self, player, out minIndex, out maxIndex);
            return;
        }

        // temporarily invert player speed Y
        player.Speed.Y *= -1;
        orig(self, player, out minIndex, out maxIndex);
        player.Speed.Y *= -1;
    }

#if TRIGGER_SPIKES_ORIGINAL
    private static Hook hook_TriggerSpikesOriginal_Added;
    private static Hook hook_TriggerSpikesOriginal_IsRiding;
    private static Hook hook_TriggerSpikesOriginal_GetPlayerCollideIndex;

#region COMMITTING MONOMOD CRIMES
    private static void LoadTriggerSpikesOriginal()
    {
        // *** WARNING: COMMITTING MONOMOD CRIMES ***
        if (typeof(TriggerSpikesOriginal) is { } type)
        {
            if (type.GetMethod(nameof(TriggerSpikesOriginal.Added), BindingFlags.Instance | BindingFlags.Public, [typeof(Scene)]) is
                { } addedMethod)
            {
                hook_TriggerSpikesOriginal_Added = new Hook(addedMethod,
                    typeof(TriggerSpikesHooks).GetMethod(nameof(TriggerSpikesOriginal_Added),
                        BindingFlags.Static | BindingFlags.NonPublic));
            }

            if (type.GetMethod(nameof(TriggerSpikesOriginal.IsRiding), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(JumpThru)]) is
                { } isRidingMethod)
            {
                hook_TriggerSpikesOriginal_IsRiding = new Hook(isRidingMethod,
                    typeof(TriggerSpikesHooks).GetMethod(nameof(TriggerSpikesOriginal_IsRiding_JumpThru),
                        BindingFlags.Static | BindingFlags.NonPublic));
            }

            if (type.GetRuntimeMethods().First(m => m.Name == nameof(TriggerSpikesOriginal.GetPlayerCollideIndex)) is
                { } getPlayerCollideIndexMethod)
            {
                hook_TriggerSpikesOriginal_GetPlayerCollideIndex = new Hook(getPlayerCollideIndexMethod,
                    typeof(TriggerSpikesHooks).GetMethod(nameof(TriggerSpikesOriginal_GetPlayerCollideIndex),
                        BindingFlags.Static | BindingFlags.NonPublic));
            }
        }
    }

    private static void UnloadTriggerSpikesOriginal()
    {
        hook_TriggerSpikesOriginal_Added?.Dispose();
        hook_TriggerSpikesOriginal_Added = null;
        hook_TriggerSpikesOriginal_IsRiding?.Dispose();
        hook_TriggerSpikesOriginal_IsRiding = null;
        hook_TriggerSpikesOriginal_GetPlayerCollideIndex?.Dispose();
        hook_TriggerSpikesOriginal_GetPlayerCollideIndex = null;
    }
#endregion

    private static void TriggerSpikesOriginal_Added(Action<TriggerSpikesOriginal, Scene> orig, TriggerSpikesOriginal self, Scene scene)
    {
        orig(self, scene);

        // we add a disabled ledge blocker for downward spikes
        if (self.direction == TriggerSpikesOriginal.Directions.Down)
        {
            self.Add(new SafeGroundBlocker());
            self.Add(new LedgeBlocker(self.UpSafeBlockCheck));
        }

        if (self.direction == TriggerSpikesOriginal.Directions.Down || self.direction == TriggerSpikesOriginal.Directions.Up)
        {
            self.Add(new PlayerGravityListener
            {
                GravityChanged = (_, args) =>
                {
                    var ledgeBlocker = self.Get<LedgeBlocker>();
                    var safeGroundBlocker = self.Get<SafeGroundBlocker>();

                    if (self.direction == TriggerSpikesOriginal.Directions.Up)
                        safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Normal;
                    else if (self.direction == TriggerSpikesOriginal.Directions.Down)
                        safeGroundBlocker.Blocking = ledgeBlocker.Blocking = args.NewValue == GravityType.Inverted;
                },
            });
        }
    }

    private static bool TriggerSpikesOriginal_IsRiding_JumpThru(Func<TriggerSpikesOriginal, JumpThru, bool> orig, TriggerSpikesOriginal self, JumpThru jumpThru)
    {
        // accept regular logic
        if (orig(self, jumpThru))
            return true;
        // also allow for down spikes to attach to gravity helper upside down jumpthrus (not maddie's)
        if (self.direction == TriggerSpikesOriginal.Directions.Down && jumpThru is UpsideDownJumpThru && self.CollideCheck(jumpThru, self.Position - Vector2.UnitY))
            return true;
        // otherwise not riding
        return false;
    }

    private delegate void TriggerSpikesOriginal_GetPlayerCollideIndex_orig(TriggerSpikesOriginal self, Player player, out int minIndex, out int maxIndex);

    private static void TriggerSpikesOriginal_GetPlayerCollideIndex(TriggerSpikesOriginal_GetPlayerCollideIndex_orig orig, TriggerSpikesOriginal self, Player player, out int minIndex, out int maxIndex)
    {
        // left and right spikes just behave as usual, as does non-inverted
        if (self.direction == TriggerSpikesOriginal.Directions.Left ||
            self.direction == TriggerSpikesOriginal.Directions.Right ||
            !GravityHelperModule.ShouldInvertPlayer)
        {
            orig(self, player, out minIndex, out maxIndex);
            return;
        }

        // temporarily invert player speed Y
        player.Speed.Y *= -1;
        orig(self, player, out minIndex, out maxIndex);
        player.Speed.Y *= -1;
    }

#endif
}
