// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SpikesHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Spikes)} hooks...");

            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide += Spikes_OnCollide;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Spikes)} hooks...");

            On.Celeste.Spikes.ctor_Vector2_int_Directions_string -= Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide -= Spikes_OnCollide;
        }

        private static void Spikes_ctor_Vector2_int_Directions_string(On.Celeste.Spikes.orig_ctor_Vector2_int_Directions_string orig, Spikes self, Vector2 position, int size, Spikes.Directions direction, string type)
        {
            orig(self, position, size, direction, type);

            // we add a disabled ledge blocker for downward spikes
            if (self.Direction == Spikes.Directions.Down)
                self.Add(new LedgeBlocker {Blocking = false});

            if (self.Direction == Spikes.Directions.Down || self.Direction == Spikes.Directions.Up)
            {
                self.Add(new GravityListener
                {
                    GravityChanged = args =>
                    {
                        var ledgeBlocker = self.Components.Get<LedgeBlocker>();
                        if (self.Direction == Spikes.Directions.Up)
                            ledgeBlocker.Blocking = args.NewValue == GravityType.Normal;
                        else if (self.Direction == Spikes.Directions.Down)
                            ledgeBlocker.Blocking = args.NewValue == GravityType.Inverted;
                    }
                });
            }
        }

        private static void Spikes_OnCollide(On.Celeste.Spikes.orig_OnCollide orig, Spikes self, Player player)
        {
            if (!GravityHelperModule.ShouldInvert || self.Direction == Spikes.Directions.Left || self.Direction == Spikes.Directions.Right)
            {
                orig(self, player);
                return;
            }

            if (self.Direction == Spikes.Directions.Up && player.Speed.Y <= 0)
                player.Die(new Vector2(0, -1));
            else if (self.Direction == Spikes.Directions.Down && player.Speed.Y >= 0 && player.Top >= self.Top)
                player.Die(new Vector2(0, 1));
        }
    }
}
