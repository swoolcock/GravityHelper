using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using MonoMod.RuntimeDetour;
using static GravityHelper.GravityModule;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace GravityHelper
{
    public static class PlayerHooks
    {
        private static GravityTypes Gravity { get { return GravityModule.Instance.Gravity; } set { GravityModule.Instance.Gravity = value; } }
        internal static GravityTypes lastGrav
        {
            get
            {
                if ((Engine.Scene as Level) != null)
                    return (GravityTypes)(Engine.Scene as Level).Session.GetCounter("jtpLastGravity");
                return GravityTypes.Normal;
            }
            set
            {
                (Engine.Scene as Level).Session.SetCounter("jtpLastGravity", (int)value);
            }

        }
        private static float fakeInvTimer;

        public static void Load()
        {
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.Update += Player_Update;
            On.Monocle.Entity.Update += Entity_Update;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            //On.Celeste.Actor.OnGround_Vector2_int += Actor_OnGround_Vector2_int;
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;

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

        private static ILHook playerUpdateSprite;
        private static void Player_UpdateSprite(ILContext il)
        {

            ILCursor cursor = new ILCursor(il);
            string getAnimFrameEdge()
            {
                return Gravity == GravityTypes.Inverted ? "idle" : "edge";
            }
            string getAnimFrameEdgeBack()
            {
                return Gravity == GravityTypes.Inverted ? "idle" : "edgeBack";
            }
            EasierILHook.ReplaceStrings(cursor, new Dictionary<string, Func<string>>() { { "edge", getAnimFrameEdge }, { "edgeBack", getAnimFrameEdgeBack } });
        }



        private static float getGravityYReverse()
        {
            return Gravity == GravityTypes.Inverted ? -1f : 1f;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);
            var onSpriteFrameChange = self.Sprite.OnFrameChange;
            self.Add(new GravityListener((GravityTypes val) =>
            {
                // Handle changing gravity inside of red boosters
                if (self.StateMachine.State == Player.StRedDash)
                {
                    self.Speed.Y = -self.Speed.Y;
                }
                if (val == GravityTypes.Inverted)
                {
                    self.Sprite.OnFrameChange = (string s) =>
                    {
                        ReflectionCache.Vector2_unitYVector.SetValue(null, -Vector2.UnitY);
                        onSpriteFrameChange(s);
                        ReflectionCache.Vector2_unitYVector.SetValue(null, -Vector2.UnitY);
                    };
                } else
                {
                    self.Sprite.OnFrameChange = onSpriteFrameChange;
                }

            }));
        }

        public static void Unload()
        {
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.Update -= Player_Update;
            On.Monocle.Entity.Update -= Entity_Update;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            //On.Celeste.Actor.OnGround_Vector2_int -= Actor_OnGround_Vector2_int;
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
            GravityTypes grav = Gravity;
            if (grav == GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                //self.LiftSpeed = -self.LiftSpeed;
            }
            bool val = orig(self);
            if (grav == GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                //self.LiftSpeed = -self.LiftSpeed;
            }
            return val;
        }

        private static bool Actor_IsRiding_Solid(On.Celeste.Actor.orig_IsRiding_Solid orig, Actor self, Solid solid)
        {
            if (self is Player && Gravity == GravityTypes.Inverted)
            {
                return self.CollideCheck(solid, self.Position - Vector2.UnitY);
            } else
            {
                return orig(self, solid);
            }
        }

        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            Gravity = lastGrav;
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target, Vector2 direction)
        {
            lastGrav = Gravity;
            if (Gravity == GravityTypes.Inverted)
            {
                self.MoveTowardsX(target.X, 60f * Engine.DeltaTime, null);
                self.MoveTowardsY(target.Y, 60f * Engine.DeltaTime, null);
                self.UpdateHair(false);
                self.UpdateCarry();
                bool flag = self.Position == target;
                bool result;
                if (flag)
                {
                    Gravity = GravityTypes.FakeInverted;

                    self.ZeroRemainderX();
                    self.ZeroRemainderY();
                    self.Speed.X = (float)((int)Math.Round((double)self.Speed.X));
                    self.Speed.Y = -(float)((int)Math.Round((double)self.Speed.Y));
                    result = true;
                }
                else
                {
                    self.Speed.Y = -60;
                    result = false;
                }
                return result;
            }
            return orig(self, target, direction);
        }

        private static void Player_BeforeUpTransition(On.Celeste.Player.orig_BeforeUpTransition orig, Player self)
        {
            if (Gravity == GravityTypes.Inverted)
            {
                fakeInvTimer = 40 * Engine.DeltaTime;
            }
            orig(self);

        }

        private static Vector2 Input_GetAimVector(On.Celeste.Input.orig_GetAimVector orig, Facings defaultFacing)
        {
            Vector2 vector2 = orig(defaultFacing);
            Player player = Engine.Scene.Tracker.GetEntity<Player>();
            if (Gravity == GravityTypes.Inverted)
            {
                vector2.Y = -vector2.Y;
            }
            return vector2;
        }

        private static void PlayerHair_AfterUpdate(On.Celeste.PlayerHair.orig_AfterUpdate orig, PlayerHair self)
        {
            if (Gravity == GravityTypes.Inverted || Gravity == GravityTypes.FakeInverted)
            {
                if (self.Scene == null)
                {
                    return;
                }
                Player player = self.Scene.Tracker.GetEntity<Player>();
                if (player == null)
                {
                    return;
                }
                DynData<PlayerHair> data = new DynData<PlayerHair>(self);

                float wave = data.Get<float>("wave");

                {
                    Vector2 value = self.Sprite.HairOffset * new Vector2((float)self.Facing, -1f);

                    float offset = player.Ducking ? 18f : 12f;
                    self.Nodes[0] = self.Sprite.RenderPosition + new Vector2(0f, (-9f * self.Sprite.Scale.Y) + offset) + value;
                    Vector2 target = self.Nodes[0] + new Vector2((float)((float)self.Facing) * self.StepInFacingPerSegment * 2f, (float)Math.Sin((double)wave) * self.StepYSinePerSegment) + self.StepPerSegment;
                    Vector2 vector = self.Nodes[0];
                    float num = 3f;
                    for (int i = 1; i < self.Sprite.HairCount; i++)
                    {
                        bool flag = i >= self.Nodes.Count;
                        if (flag)
                        {
                            self.Nodes.Add(self.Nodes[i - 1]);
                        }
                        bool simulateMotion = self.SimulateMotion;
                        if (simulateMotion)
                        {
                            float num2 = (1f - (float)i / (float)self.Sprite.HairCount * 0.5f) * -self.StepApproach;
                            self.Nodes[i] = Calc.Approach(self.Nodes[i], target, num2 * Engine.DeltaTime);
                        }
                        bool flag2 = (self.Nodes[i] - vector).Length() > num;
                        if (flag2)
                        {
                            self.Nodes[i] = vector + (self.Nodes[i] - vector).SafeNormalize() * num;
                        }
                        target = self.Nodes[i] + new Vector2((float)(-(float)self.Facing) * self.StepInFacingPerSegment, (float)Math.Sin((double)(wave + (float)i * 0.8f)) * self.StepYSinePerSegment) + self.StepPerSegment;
                        vector = self.Nodes[i];
                    }
                }
            }
            else
            {
                orig(self);
            }


        }

        private static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self)
        {
            if (Gravity == GravityTypes.Inverted || Gravity == GravityTypes.FakeInverted)
            {
                PlayerSprite sprite = self.Sprite;
                if (!sprite.HasHair)
                {
                    return;
                }
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
            if (Gravity == GravityTypes.Inverted)
            {
                CheckInvGround(self, false);
            }
            return orig(self);
        }

        private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
        {
            if (Gravity == GravityTypes.Inverted)
            {
                if (onInvGround)
                {
                    orig(self, particles, playSfx);
                    self.Speed.Y = 105f;
                }
            }
            else
            {
                orig(self, particles, playSfx);
            }

        }

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
        {
            bool condition = (self is Player && Gravity == GravityTypes.Inverted);
            if (condition)
            {
                downCheck = -downCheck;
            }
            bool ret = orig(self, downCheck);
            return ret;
        }

        private static void Entity_Update(On.Monocle.Entity.orig_Update orig, Entity self)
        {
            if (self is Player)
            {
                if (Gravity == GravityTypes.Inverted)
                    ((Player)self).Speed.Y = -((Player)self).Speed.Y;
            }
            orig(self);
            if (self is Player)
            {
                if (Gravity == GravityTypes.Inverted)
                    ((Player)self).Speed.Y = -((Player)self).Speed.Y;
            }
        }

        public static void CheckInvGround(Player self, bool recoverDash = false, bool canUnDuck = false)
        {
            DynData<Player> data = new DynData<Player>(self);
            float dashRefillCooldownTimer = (float)data["dashRefillCooldownTimer"];
            if (recoverDash && dashRefillCooldownTimer > 0f && SaveData.Instance.Assists.DashMode == Assists.DashModes.Infinite && !self.SceneAs<Level>().InCutscene)
            {
                self.RefillDash();
            }
            if (self.Speed.Y >= 0f)
            {
                Celeste.Platform platform = self.CollideFirst<Solid>(self.Position - Vector2.UnitY);
                bool flag13 = platform == null;
                if (flag13)
                {
                    platform = self.CollideFirstOutside<JumpThru>(self.Position - Vector2.UnitY);
                }
                bool flag14 = platform != null;

                if (flag14)
                {
                    data.Set("jumpGraceTimer", 0.1f);
                    data.Set("onGround", true);
                    onInvGround = true;
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
                    onInvGround = false;
                    ReflectionCache.Player_OnSafeGround.SetValue(self, false);
                }
            }
            else
            {
                data.Set("onGround", false);
                onInvGround = false;
            }
        }

        public static bool onInvGround;
        //static bool playerUpdate;
        //static bool lastNoRefills;
        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            DynData<Player> data = new DynData<Player>(self);

            int lastDashes = self.Dashes;
            bool lastDucking = self.Ducking;
            if (Gravity == GravityTypes.Inverted)
            {
                self.Speed.Y = -self.Speed.Y;
                // fixes bugs where hitting a "ceiling" while upside-down would recover your dash
                data.Set("dashRefillCooldownTimer", 0.1f);
                self.LiftSpeed = -self.LiftSpeed;
            }

            orig(self);

            if (Gravity == GravityTypes.Inverted)
            {
                // Handle un-ducking
                CheckInvGround(self, self.Dashes == lastDashes, true);
                if (lastDucking == false && self.Ducking == true)
                {
                    self.Position.Y -= 5f;
                    while (self.CollideFirst<Solid>(self.Position) != null)
                    {
                        self.Position.Y += 1f;
                    }
                }

                self.Speed.Y = -self.Speed.Y;
                self.LiftSpeed = -self.LiftSpeed;
                if (self.Position.Y < self.SceneAs<Level>().Bounds.Top)
                {
                    self.Die(Vector2.Zero);
                }
            }

        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            if (Gravity == GravityTypes.Inverted || Gravity == GravityTypes.FakeInverted)
            {
                FlipScale(self);
                if (self.Ducking)
                {
                    self.Sprite.Y -= 6f;
                }
                else
                {
                    self.Sprite.Y -= 11f;
                }
                if (self.StateMachine.State == Player.StNormal && Gravity == GravityTypes.FakeInverted)
                {
                    fakeInvTimer -= Engine.DeltaTime;
                    if (fakeInvTimer <= 0f)
                    {
                        if (self.Speed.Y < 0)
                            self.Speed.Y = -self.Speed.Y;
                        Gravity = GravityTypes.Inverted;
                    }
                }
            }

            orig(self);
            if (Gravity == GravityTypes.Inverted || Gravity == GravityTypes.FakeInverted)
            {
                FlipScale(self);
                if (self.Ducking)
                {
                    self.Sprite.Y += 6f;
                }
                else
                {
                    self.Sprite.Y += 11f;
                }
            }
        }

        public static void FlipScale(Player player)
        {
            //player.Sprite.Scale.Y = -player.Sprite.Scale.Y;
            player.Hair.Sprite.Scale.Y = -player.Hair.Sprite.Scale.Y;
        }
    }
}
