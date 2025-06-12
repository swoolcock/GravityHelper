// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;

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
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(TriggerSpikes)} hooks...");

        On.Celeste.TriggerSpikes.IsRiding_JumpThru -= TriggerSpikes_IsRiding_JumpThru;
        On.Celeste.TriggerSpikes.ctor_Vector2_int_Directions -= TriggerSpikes_ctor_Vector2_int_Directions;
        On.Celeste.TriggerSpikes.GetPlayerCollideIndex -= TriggerSpikes_GetPlayerCollideIndex;
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
}
