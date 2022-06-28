// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SpikesHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Spikes)} hooks...");

            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
            using (new DetourContext { Before = { "*" } })
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
                self.Add(new PlayerGravityListener
                {
                    GravityChanged = (_, args) =>
                    {
                        var ledgeBlocker = self.Components.Get<LedgeBlocker>();
                        if (self.Direction == Spikes.Directions.Up)
                            ledgeBlocker.Blocking = args.NewValue == GravityType.Normal;
                        else if (self.Direction == Spikes.Directions.Down)
                            ledgeBlocker.Blocking = args.NewValue == GravityType.Inverted;
                    },
                });
            }
        }

        private static void Spikes_OnCollide(On.Celeste.Spikes.orig_OnCollide orig, Spikes self, Player player)
        {
            // left and right spikes just behave as usual
            if (self.Direction == Spikes.Directions.Left || self.Direction == Spikes.Directions.Right)
            {
                orig(self, player);
                return;
            }

            // if we're not inverting and not dream dashing, just behave as usual
            var invert = GravityHelperModule.ShouldInvertPlayer;
            var isDreamDash = player.StateMachine.State == Player.StDreamDash;
            if (!invert && !isDreamDash)
            {
                orig(self, player);
                return;
            }

            // for nicer feeling gameplay, we always apply the "spikes on top of a dream block don't kill you"
            // likewise, we apply this check to spikes on the bottom if Madeline changed gravity entering the dream block

            if (self.Direction == Spikes.Directions.Up)
            {
                // regular upward spikes check that includes Y position
                if (!invert && player.Speed.Y >= 0 && player.Bottom <= self.Bottom)
                    player.Die(-Vector2.UnitY);
                // inverted upward spikes check that only includes Y position if we're dream dashing
                else if (invert && player.Speed.Y <= 0 && (!isDreamDash || player.Bottom <= self.Bottom))
                    player.Die(-Vector2.UnitY);
            }
            else
            {
                var changedGravity = GravityHelperModule.PlayerComponent?.PreDreamBlockGravityType != GravityHelperModule.PlayerComponent?.CurrentGravity;
                // regular downward spikes check that only includes Y position if we changed gravity on entry
                if (!invert && player.Speed.Y <= 0 && (!changedGravity || player.Top >= self.Top))
                    player.Die(Vector2.UnitY);
                // inverted downward spikes check that includes Y position
                else if (invert && player.Speed.Y >= 0 && player.Top >= self.Top)
                    player.Die(Vector2.UnitY);
            }
        }
    }
}
