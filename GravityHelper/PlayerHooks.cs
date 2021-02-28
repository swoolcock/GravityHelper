using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Celeste;
using Platform = Celeste.Platform;

namespace GravityHelper
{
    public static class PlayerHooks
    {
        private static GravityHelperModule.GravityTypes gravity
        {
            get => GravityHelperModule.Instance.Gravity;
            set => GravityHelperModule.Instance.Gravity = value;
        }

        internal static GravityHelperModule.GravityTypes LastGrav
        {
            get => Engine.Scene is Level level ? (GravityHelperModule.GravityTypes)level.Session.GetCounter(Constants.LastGravityCounterKey) : GravityHelperModule.GravityTypes.Normal;
            set => (Engine.Scene as Level)?.Session.SetCounter(Constants.LastGravityCounterKey, (int)value);
        }

        private static float fakeInvTimer;

        public static void Load()
        {
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.Update += Player_Update;
            On.Monocle.Entity.Update += Entity_Update;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            On.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;

            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.PlayerHair.Render += PlayerHair_Render;
            On.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
            On.Celeste.Input.GetAimVector += Input_GetAimVector;
            On.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;
            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            On.Celeste.Player.Die += Player_Die;

            // Standing on zippers
            On.Celeste.Actor.IsRiding_Solid += Actor_IsRiding_Solid;

            // Getting launched from moving solids
            On.Celeste.Player.LaunchedBoostCheck += Player_LaunchedBoostCheck;

            // Various collision checks
            playerUpdateSprite = new ILHook(typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic), Player_UpdateSprite);
        }

        private static int Player_ClimbUpdate(On.Celeste.Player.orig_ClimbUpdate orig, Player self)
        {
            if (!GravityHelperModule.Settings.Enabled || gravity == GravityHelperModule.GravityTypes.Normal)
                return orig(self);

            Input.MoveY.Value *= -1;
            var rv = orig(self);
            Input.MoveY.Value *= -1;

            return rv;
        }

        private static ILHook playerUpdateSprite;

        private static void Player_UpdateSprite(ILContext il)
        {
            string getAnimFrameEdge() => gravity == GravityHelperModule.GravityTypes.Inverted ? "idle" : "edge";
            string getAnimFrameEdgeBack() => gravity == GravityHelperModule.GravityTypes.Inverted ? "idle" : "edgeBack";

            var cursor = new ILCursor(il);
            cursor.ReplaceStrings(new Dictionary<string, Func<string>> { { "edge", getAnimFrameEdge }, { "edgeBack", getAnimFrameEdgeBack } });
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);
            var onSpriteFrameChange = self.Sprite.OnFrameChange;
            self.Add(new GravityListener(val =>
            {
                // Handle changing gravity inside of red boosters
                if (self.StateMachine.State == Player.StRedDash)
                    self.Speed.Y = -self.Speed.Y;

                if (val == GravityHelperModule.GravityTypes.Inverted)
                {
                    self.Sprite.OnFrameChange = s =>
                    {
                        ReflectionCache.Vector2_unitYVector.SetValue(null, -Vector2.UnitY);
                        onSpriteFrameChange(s);
                        ReflectionCache.Vector2_unitYVector.SetValue(null, -Vector2.UnitY);
                    };
                }
                else
                    self.Sprite.OnFrameChange = onSpriteFrameChange;
            }));
        }

        public static void Unload()
        {
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.Update -= Player_Update;
            On.Monocle.Entity.Update -= Entity_Update;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            On.Celeste.Player.Jump -= Player_Jump;
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            On.Celeste.PlayerHair.Render -= PlayerHair_Render;
            On.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
            On.Celeste.Input.GetAimVector -= Input_GetAimVector;
            On.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;
            On.Celeste.Player.TransitionTo -= Player_TransitionTo;

            On.Celeste.Player.Die -= Player_Die;
            // Standing on zippers
            On.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding_Solid;

            // Getting launched from moving solids
            On.Celeste.Player.LaunchedBoostCheck -= Player_LaunchedBoostCheck;

            playerUpdateSprite.Undo();
        }

        private static bool Player_LaunchedBoostCheck(On.Celeste.Player.orig_LaunchedBoostCheck orig, Player self)
        {
            GravityHelperModule.GravityTypes grav = gravity;

            if (grav == GravityHelperModule.GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                //self.LiftSpeed = -self.LiftSpeed;
            }

            bool val = orig(self);

            if (grav == GravityHelperModule.GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                //self.LiftSpeed = -self.LiftSpeed;
            }

            return val;
        }

        private static bool Actor_IsRiding_Solid(On.Celeste.Actor.orig_IsRiding_Solid orig, Actor self, Solid solid) =>
            self is Player && gravity == GravityHelperModule.GravityTypes.Inverted ? self.CollideCheck(solid, self.Position - Vector2.UnitY) : orig(self, solid);

        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            gravity = LastGrav;
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target, Vector2 direction)
        {
            LastGrav = gravity;

            if (gravity != GravityHelperModule.GravityTypes.Inverted)
                return orig(self, target, direction);

            self.MoveTowardsX(target.X, 60f * Engine.DeltaTime);
            self.MoveTowardsY(target.Y, 60f * Engine.DeltaTime);
            self.UpdateHair(false);
            self.UpdateCarry();
            bool flag = self.Position == target;
            bool result;

            if (flag)
            {
                gravity = GravityHelperModule.GravityTypes.FakeInverted;

                self.ZeroRemainderX();
                self.ZeroRemainderY();
                self.Speed.X = (int)Math.Round(self.Speed.X);
                self.Speed.Y = -(float)(int)Math.Round(self.Speed.Y);
                result = true;
            }
            else
            {
                self.Speed.Y = -60;
                result = false;
            }

            return result;
        }

        private static void Player_BeforeUpTransition(On.Celeste.Player.orig_BeforeUpTransition orig, Player self)
        {
            if (gravity == GravityHelperModule.GravityTypes.Inverted)
                fakeInvTimer = 40 * Engine.DeltaTime;

            orig(self);
        }

        private static Vector2 Input_GetAimVector(On.Celeste.Input.orig_GetAimVector orig, Facings defaultFacing)
        {
            Vector2 vector2 = orig(defaultFacing);
            if (gravity == GravityHelperModule.GravityTypes.Inverted)
                vector2.Y = -vector2.Y;

            return vector2;
        }

        private static void PlayerHair_AfterUpdate(On.Celeste.PlayerHair.orig_AfterUpdate orig, PlayerHair self)
        {
            if (gravity == GravityHelperModule.GravityTypes.Inverted || gravity == GravityHelperModule.GravityTypes.FakeInverted)
            {
                Player player = self.Scene?.Tracker.GetEntity<Player>();
                if (player == null) return;

                DynData<PlayerHair> data = new DynData<PlayerHair>(self);

                float wave = data.Get<float>("wave");

                {
                    Vector2 value = self.Sprite.HairOffset * new Vector2((float)self.Facing, -1f);

                    float offset = player.Ducking ? 18f : 12f;
                    self.Nodes[0] = self.Sprite.RenderPosition + new Vector2(0f, -9f * self.Sprite.Scale.Y + offset) + value;
                    Vector2 target = self.Nodes[0] + new Vector2((float)self.Facing * self.StepInFacingPerSegment * 2f, (float)Math.Sin(wave) * self.StepYSinePerSegment) + self.StepPerSegment;
                    Vector2 vector = self.Nodes[0];

                    const float num = 3f;

                    for (int i = 1; i < self.Sprite.HairCount; i++)
                    {
                        if (i >= self.Nodes.Count)
                            self.Nodes.Add(self.Nodes[i - 1]);

                        if (self.SimulateMotion)
                        {
                            float num2 = (1f - i / (float)self.Sprite.HairCount * 0.5f) * -self.StepApproach;
                            self.Nodes[i] = Calc.Approach(self.Nodes[i], target, num2 * Engine.DeltaTime);
                        }

                        if ((self.Nodes[i] - vector).Length() > num)
                            self.Nodes[i] = vector + (self.Nodes[i] - vector).SafeNormalize() * num;

                        target = self.Nodes[i] + new Vector2(-(float)self.Facing * self.StepInFacingPerSegment, (float)Math.Sin(wave + i * 0.8f) * self.StepYSinePerSegment) + self.StepPerSegment;
                        vector = self.Nodes[i];
                    }
                }
            }
            else
                orig(self);
        }

        private static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self)
        {
            if (gravity == GravityHelperModule.GravityTypes.Inverted || gravity == GravityHelperModule.GravityTypes.FakeInverted)
            {
                PlayerSprite sprite = self.Sprite;
                if (!sprite.HasHair)
                    return;

                Vector2 origin = new Vector2(5f, -1f);
                Color color = self.Border * self.Alpha;

                if (self.DrawPlayerSpriteOutline)
                {
                    Color color2 = sprite.Color;
                    Vector2 position = sprite.Position;
                    sprite.Color = color;
                    sprite.Position = position + new Vector2(0f, -1f);
                    sprite.Render();
                    sprite.Position = position + new Vector2(0f, 1f);
                    sprite.Render();
                    sprite.Position = position + new Vector2(-1f, 0f);
                    sprite.Render();
                    sprite.Position = position + new Vector2(1f, 0f);
                    sprite.Render();
                    sprite.Color = color2;
                    sprite.Position = position;
                }

                self.Nodes[0] = self.Nodes[0].Floor();

                if (color.A > 0)
                {
                    for (int i = 0; i < sprite.HairCount; i++)
                    {
                        MTexture hairTexture = self.GetHairTexture(i);
                        Vector2 hairScale = self.GetHairScale(i);
                        hairScale.Y = -hairScale.Y;
                        hairTexture.Draw(self.Nodes[i] + new Vector2(-1f, 0f), origin, color, hairScale);
                        hairTexture.Draw(self.Nodes[i] + new Vector2(1f, 0f), origin, color, hairScale);
                        hairTexture.Draw(self.Nodes[i] + new Vector2(0f, -1f), origin, color, hairScale);
                        hairTexture.Draw(self.Nodes[i] + new Vector2(0f, 1f), origin, color, hairScale);
                    }
                }

                for (int j = sprite.HairCount - 1; j >= 0; j--)
                {
                    Vector2 hairScale = self.GetHairScale(j);
                    hairScale.Y = -hairScale.Y;
                    self.GetHairTexture(j).Draw(self.Nodes[j], origin, self.GetHairColor(j), hairScale);
                }
            }
            else
            {
                //self.MoveHairBy(new Vector2(0f, -16f));
                orig(self);
            }
        }

        private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
        {
            if (!GravityHelperModule.Settings.Enabled || gravity == GravityHelperModule.GravityTypes.Normal)
                return orig(self);

            CheckInvGround(self);
            var rv = orig(self);
            return rv;
        }

        private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
        {
            if (!GravityHelperModule.Settings.Enabled || gravity == GravityHelperModule.GravityTypes.Normal)
            {
                orig(self, particles, playSfx);
                return;
            }

            if (!OnInvGround) return;

            orig(self, particles, playSfx);
            self.Speed.Y = 105f;
        }

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
        {
            if (self is Player && gravity == GravityHelperModule.GravityTypes.Inverted)
                downCheck = -downCheck;

            return orig(self, downCheck);
        }

        private static void Entity_Update(On.Monocle.Entity.orig_Update orig, Entity self)
        {
            var player = self as Player;

            if (player != null)
            {
                if (gravity == GravityHelperModule.GravityTypes.Inverted)
                    player.Speed.Y = -player.Speed.Y;
            }

            orig(self);

            if (player != null)
            {
                if (gravity == GravityHelperModule.GravityTypes.Inverted)
                    player.Speed.Y = -player.Speed.Y;
            }
        }

        public static void CheckInvGround(Player self, bool recoverDash = false, bool canUnDuck = false)
        {
            DynData<Player> data = new DynData<Player>(self);
            float dashRefillCooldownTimer = (float)data["dashRefillCooldownTimer"];

            if (recoverDash && dashRefillCooldownTimer > 0f && SaveData.Instance.Assists.DashMode == Assists.DashModes.Infinite && !self.SceneAs<Level>().InCutscene)
                self.RefillDash();

            if (self.Speed.Y >= 0f)
            {
                Platform platform = self.CollideFirst<Solid>(self.Position - Vector2.UnitY)
                                    ?? (Platform)self.CollideFirstOutside<JumpThru>(self.Position - Vector2.UnitY);

                if (platform != null)
                {
                    data.Set("jumpGraceTimer", 0.1f);
                    data.Set("onGround", true);
                    OnInvGround = true;

                    if (recoverDash && dashRefillCooldownTimer > 0f)
                    {
                        self.RefillStamina();
                        self.RefillDash();
                    }

                    if (self.Ducking && canUnDuck && Input.MoveY.Value != 1)
                    {
                        self.Ducking = false;
                        self.Position.Y += 5f;
                    }

                    self.LiftSpeed = Vector2.Zero;
                    ReflectionCache.Player_OnSafeGround.SetValue(self, true);
                }
                else
                {
                    data.Set("jumpGraceTimer", 0f);
                    data.Set("onGround", false);
                    OnInvGround = false;
                    ReflectionCache.Player_OnSafeGround.SetValue(self, false);
                }
            }
            else
            {
                data.Set("onGround", false);
                OnInvGround = false;
            }
        }

        public static bool OnInvGround;

        //static bool playerUpdate;
        //static bool lastNoRefills;

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            DynData<Player> data = new DynData<Player>(self);

            int lastDashes = self.Dashes;
            bool lastDucking = self.Ducking;

            if (gravity == GravityHelperModule.GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                // fixes bugs where hitting a "ceiling" while upside-down would recover your dash
                data.Set("dashRefillCooldownTimer", 0.1f);
                self.LiftSpeed = -self.LiftSpeed;
            }

            orig(self);

            if (gravity == GravityHelperModule.GravityTypes.Inverted)
            {
                // Handle un-ducking
                CheckInvGround(self, self.Dashes == lastDashes, true);

                if (lastDucking == false && self.Ducking)
                {
                    self.Position.Y -= 5f;
                    while (self.CollideFirst<Solid>(self.Position) != null)
                        self.Position.Y += 1f;
                }

                self.Speed.Y = -self.Speed.Y;
                self.LiftSpeed = -self.LiftSpeed;

                if (self.Position.Y < self.SceneAs<Level>().Bounds.Top)
                    self.Die(Vector2.Zero);
            }
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            if (gravity == GravityHelperModule.GravityTypes.Inverted || gravity == GravityHelperModule.GravityTypes.FakeInverted)
            {
                FlipScale(self);
                self.Sprite.Y -= self.Ducking ? 6f : 11f;

                if (self.StateMachine.State == Player.StNormal && gravity == GravityHelperModule.GravityTypes.FakeInverted)
                {
                    fakeInvTimer -= Engine.DeltaTime;

                    if (fakeInvTimer <= 0f)
                    {
                        self.Speed.Y = Math.Abs(self.Speed.Y);
                        gravity = GravityHelperModule.GravityTypes.Inverted;
                    }
                }
            }

            orig(self);

            if (gravity == GravityHelperModule.GravityTypes.Inverted || gravity == GravityHelperModule.GravityTypes.FakeInverted)
            {
                FlipScale(self);
                self.Sprite.Y += self.Ducking ? 6f : 11f;
            }
        }

        public static void FlipScale(Player player)
        {
            //player.Sprite.Scale.Y = -player.Sprite.Scale.Y;
            player.Hair.Sprite.Scale.Y = -player.Hair.Sprite.Scale.Y;
        }
    }
}
