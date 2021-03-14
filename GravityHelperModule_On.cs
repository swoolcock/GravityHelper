using System.Collections;
using Celeste;
using GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using Spikes = On.Celeste.Spikes;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule
    {
        private static void loadOnHooks()
        {
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Level.Update += Level_Update;
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.SlipCheck += Player_SlipCheck;
            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
        }

        private static void unloadOnHooks()
        {
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            On.Celeste.Level.Update -= Level_Update;
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.SlipCheck -= Player_SlipCheck;
            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string -= Spikes_ctor_Vector2_int_Directions_string;
        }

        private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (Settings.ToggleInvertGravity.Pressed)
            {
                Settings.ToggleInvertGravity.ConsumePress();
                if (Settings.Enabled)
                    Session.Gravity = GravityType.Toggle;
            }

            orig(self);
        }

        private static void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            if (!ShouldInvert)
            {
                orig(self, move);
                return;
            }

            solidMoving = true;
            orig(self, move);
            solidMoving = false;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position,
            PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            self.Add(new TransitionListener
            {
                OnOutBegin = () => transitioning = true,
                OnInEnd = () => transitioning = false
            }, new GravityListener());
        }

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck) =>
            orig(self, ShouldInvert && self is Player ? -downCheck : downCheck);

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (!ShouldInvert)
            {
                orig(self);
                return;
            }

            var aimY = Input.Aim.Value.Y;
            var moveY = Input.MoveY.Value;

            setVirtualJoystickValue(new Vector2(Input.Aim.Value.X, -aimY));
            Input.MoveY.Value = -moveY;

            orig(self);

            Input.MoveY.Value = moveY;
            setVirtualJoystickValue(new Vector2(Input.Aim.Value.X, aimY));
        }

        private static IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self,
            LevelData next, Vector2 direction)
        {
            transitioning = true;
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext())
                yield return origEnum.Current;
            transitioning = false;
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target,
            Vector2 direction)
        {
            if (Settings.Enabled)
                Session.PreviousGravity = Session.Gravity;

            transitioning = true;
            bool val = orig(self, target, direction);
            transitioning = false;
            return val;
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            var scaleY = self.Sprite.Scale.Y;

            if (ShouldInvert)
                self.Sprite.Scale.Y = -scaleY;

            orig(self);

            if (ShouldInvert)
                self.Sprite.Scale.Y = scaleY;
        }

        private static Vector2 PlayerHair_GetHairScale(On.Celeste.PlayerHair.orig_GetHairScale orig, PlayerHair self, int index)
        {
            if (self == null) return Vector2.One;

            var scale = orig(self, index);
            if (ShouldInvert)
                scale.Y *= -1;
            return scale;
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            // CollideFirst<T> will crash if no such entity exists (we really need a CollideFirstOrDefault<T>!)
            SpawnGravityTrigger trigger = scene.Entities.AmountOf<SpawnGravityTrigger>() > 0 ? self.CollideFirst<SpawnGravityTrigger>() : null;
            Session.Gravity = trigger?.GravityType ?? Session.PreviousGravity;
        }

        private static void Spikes_ctor_Vector2_int_Directions_string(Spikes.orig_ctor_Vector2_int_Directions_string orig, Celeste.Spikes self, Vector2 position, int size, Celeste.Spikes.Directions direction, string type)
        {
            orig(self, position, size, direction, type);

            // we add a disabled ledge blocker for downward spikes
            if (self.Direction == Celeste.Spikes.Directions.Down)
                self.Add(new LedgeBlocker { Blocking = false });

            self.Add(new GravityListener());
        }

        private static bool Player_SlipCheck(On.Celeste.Player.orig_SlipCheck orig, Player self, float addY)
        {
            if (!ShouldInvert)
                return orig(self, addY);

            Vector2 point = self.Facing != Facings.Right ? self.BottomLeft - Vector2.UnitX - Vector2.UnitY * (4f + addY) : self.BottomRight - Vector2.UnitY * (4f + addY);
            return !self.Scene.CollideCheck<Solid>(point) && !self.Scene.CollideCheck<Solid>(point - Vector2.UnitY * (addY - 4f));
        }
    }
}