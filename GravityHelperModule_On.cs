using System;
using Celeste;
using GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using Spikes = On.Celeste.Spikes;
using Spring = On.Celeste.Spring;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule
    {
        private static void loadOnHooks()
        {
            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Level.Update += Level_Update;
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;
            On.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
            On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.ReflectBounce += Player_ReflectBounce;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.SlipCheck += Player_SlipCheck;
            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spring.OnCollide += Spring_OnCollide;
        }

        private static void unloadOnHooks()
        {
            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            On.Celeste.Level.Update -= Level_Update;
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;
            On.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
            On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.ReflectBounce -= Player_ReflectBounce;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.SlipCheck -= Player_SlipCheck;
            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string -= Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;
        }

        private static void Player_ReflectBounce(On.Celeste.Player.orig_ReflectBounce orig, Player self, Vector2 direction) =>
            orig(self, ShouldInvert ? new Vector2(direction.X, -direction.Y) : direction);

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
        {
            if (!ShouldInvert)
                return orig(self);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            var rv = orig(self);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            return rv;
        }

        private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir)
        {
            if (!ShouldInvert)
                return orig(self, dir);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            var rv = orig(self, new Vector2(dir.X, -dir.Y));

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            return rv;
        }

        private static void Spring_OnCollide(Spring.orig_OnCollide orig, Celeste.Spring self, Player player)
        {
            if (!ShouldInvert)
            {
                orig(self, player);
                return;
            }

            // check copied from orig
            if (player.StateMachine.State == Player.StDreamDash || !self.GetPlayerCanUse())
                return;

            // if we hit a floor spring while inverted, flip gravity back to normal
            if (self.Orientation == Celeste.Spring.Orientations.Floor)
                Session.Gravity = GravityType.Normal;

            orig(self, player);
        }

        private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int movev, Collision oncollide, Solid pusher)
        {
            var shouldInvert = self is Player player
                               && player.StateMachine.State != Player.StDreamDash
                               && player.CurrentBooster == null
                               && !solidMoving && !transitioning
                               && ShouldInvert;
            return orig(self, shouldInvert ? -movev : movev, oncollide, pusher);
        }

        private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (Settings.ToggleInvertGravity.Pressed)
            {
                Settings.ToggleInvertGravity.ConsumePress();
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
                OnOutBegin = () => Session.PreviousGravity = Session.Gravity,
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

            Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, -aimY));
            Input.MoveY.Value = -moveY;

            orig(self);

            Input.MoveY.Value = moveY;
            Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, aimY));
        }

        private static void Player_BeforeDownTransition(On.Celeste.Player.orig_BeforeDownTransition orig, Player self)
        {
            if (!ShouldInvert)
            {
                orig(self);
                return;
            }

            // FIXME: copied from Player.BeforeUpTransition - we never call orig!
            self.Speed.X = 0.0f;
            if (self.StateMachine.State != Player.StRedDash && self.StateMachine.State != Player.StReflectionFall && self.StateMachine.State != Player.StStarFly)
            {
                self.SetVarJumpSpeed(self.Speed.Y = -105f);
                self.StateMachine.State = self.StateMachine.State != Player.StSummitLaunch ? Player.StNormal : Player.StIntroJump;
                self.AutoJump = true;
                self.AutoJumpTimer = 0.0f;
                self.SetVarJumpTimer(0.2f);
            }
            self.SetDashCooldownTimer(0.2f);
        }

        private static void Player_BeforeUpTransition(On.Celeste.Player.orig_BeforeUpTransition orig, Player self)
        {
            if (!ShouldInvert)
            {
                orig(self);
                return;
            }

            // FIXME: copied from Player.BeforeDownTransition - we never call orig!
            if (self.StateMachine.State != Player.StRedDash && self.StateMachine.State != Player.StReflectionFall && self.StateMachine.State != Player.StStarFly)
            {
                self.StateMachine.State = Player.StNormal;
                self.Speed.Y = Math.Max(0.0f, self.Speed.Y);
                self.AutoJump = false;
                self.SetVarJumpTimer(0.0f);
            }
            foreach (Entity entity in self.Scene.Tracker.GetEntities<Platform>())
            {
                if (!(entity is SolidTiles) && self.CollideCheckOutside(entity, self.Position - Vector2.UnitY * self.Height))
                    entity.Collidable = false;
            }
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target,
            Vector2 direction)
        {
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

            SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
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