using System;
using Celeste;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace GravityHelper
{
    public static class MiscHooks
    {
        private static IDetour hook_Level_orig_TransitionRoutine;

        public static void Load()
        {
            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding;
            IL.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
            IL.Celeste.PlayerHair.Render += PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;

            On.Celeste.Actor.MoveV += Actor_MoveV;
            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
            On.Celeste.Level.Update += Level_Update;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
            On.Celeste.SolidTiles.GetLandSoundIndex += SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex += SolidTiles_GetStepSoundIndex;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide += Spikes_OnCollide;
            On.Celeste.Spring.OnCollide += Spring_OnCollide;

            hook_Level_orig_TransitionRoutine = new ILHook(ReflectionCache.Level_OrigTransitionRoutine.GetStateMachineTarget(), Level_orig_TransitionRoutine);
        }

        public static void Unload()
        {
            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
            IL.Celeste.PlayerHair.Render -= PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;

            On.Celeste.Actor.MoveV -= Actor_MoveV;
            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
            On.Celeste.Level.Update -= Level_Update;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
            On.Celeste.SolidTiles.GetLandSoundIndex -= SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex -= SolidTiles_GetStepSoundIndex;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string -= Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide -= Spikes_OnCollide;
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;

            hook_Level_orig_TransitionRoutine?.Dispose();
            hook_Level_orig_TransitionRoutine = null;
        }

        #region IL Hooks

        private static void Actor_IsRiding(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNextAddition();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Vector2, Actor, Vector2>>((v, a) =>
                GravityHelperModule.ShouldInvert && a is Player ? new Vector2(v.X, -v.Y) : v);
        }

        private static void Bumper_OnPlayer(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
            {
                if (!GravityHelperModule.ShouldInvert) return v;
                return new Vector2(v.X,  p.CenterY - (v.Y - p.CenterY));
            });
        }

        private static void Level_orig_TransitionRoutine(ILContext il)
        {
            var cursor = new ILCursor(il);

            //// if (direction == Vector2.UnitY)
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Equality"));
            cursor.EmitInvertVectorDelegate();

            //// --playerTo.Y;
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(1) && instr.Next.MatchSub());
            cursor.EmitInvertFloatDelegate();
            cursor.GotoPrev(instr => instr.MatchLdarg(0));

            //// while ((double) direction.X != 0.0 && (double) playerTo.Y >= (double) level.Bounds.Bottom)
            // to avoid changing the >= comparison, this converts to -playerTo.Y >= -level.Bounds.Top
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
            cursor.GotoNext(instr => instr.MatchCall<Rectangle>("get_Bottom"));
            cursor.EmitDelegate<Func<bool>>(() => GravityHelperModule.ShouldInvert);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Rectangle).GetMethod("get_Top"));
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static void PlayerHair_Render(ILContext il)
        {
            var cursor = new ILCursor(il);

            void emitChangePositionDelegate()
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Vector2, PlayerHair, Vector2>>((v, hair) =>
                {
                    if (GravityHelperModule.ShouldInvert && hair.Entity is Player player)
                    {
                        return player.StateMachine.State != Player.StStarFly
                            ? new Vector2(v.X, 2 * player.Position.Y - v.Y)
                            : new Vector2(v.X, v.Y + 2 * player.GetNormalHitbox().CenterY);
                    }
                    return v;
                });
            }

            // invert this.GetHairScale(index);
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
            cursor.EmitInvertVectorDelegate();

            for (int i = 0; i < 4; i++)
            {
                // match hairTexture.Draw
                cursor.GotoNext(instr => instr.MatchCallvirt<MTexture>(nameof(MTexture.Draw)));
                cursor.GotoPrev(instr => instr.MatchLdcR4(out _));
                cursor.Index--;
                // adjust this.Nodes[index]
                emitChangePositionDelegate();
                cursor.GotoNext(MoveType.After,instr => instr.MatchCallvirt<MTexture>(nameof(MTexture.Draw)));
            }

            // this.GetHairTexture(index).Draw(this.Nodes[index], origin, this.GetHairColor(index), this.GetHairScale(index));
            cursor.GotoNext(instr => instr.MatchCallvirt<PlayerHair>(nameof(PlayerHair.GetHairColor)));
            cursor.GotoPrev(instr => instr.MatchLdloc(1));
            emitChangePositionDelegate();
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
            cursor.EmitInvertVectorDelegate();
        }

        private static void Solid_GetPlayerOnTop(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNextSubtraction();
            cursor.EmitInvertVectorDelegate();
        }

        #endregion

        #region On Hooks

        private static bool Actor_MoveV(On.Celeste.Actor.orig_MoveV orig, Actor self, float moveV, Collision onCollide, Solid pusher)
        {
            if (!GravityHelperModule.ShouldInvertActor(self))
                return orig(self, moveV, onCollide, pusher);

            var movementCounter = (Vector2)ReflectionCache.Actor_MovementCounter.GetValue(self);
            movementCounter.Y -= moveV;

            int moveV1 = (int) Math.Round(movementCounter.Y, MidpointRounding.ToEven);
            if (moveV1 == 0)
            {
                ReflectionCache.Actor_MovementCounter.SetValue(self, movementCounter);
                return false;
            }

            movementCounter.Y -= moveV1;
            ReflectionCache.Actor_MovementCounter.SetValue(self, movementCounter);

            return self.MoveVExact(-moveV1, onCollide, pusher);
        }

        private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher) =>
            orig(self, GravityHelperModule.ShouldInvertActor(self) ? -moveV : moveV, onCollide, pusher);

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
        {
            if (!GravityHelperModule.ShouldInvert || !(self is Player))
                return orig(self, downCheck);

            if (self.CollideCheck<Solid>(self.Position - Vector2.UnitY * downCheck))
                return true;

            var udjtType = ReflectionCache.UpsideDownJumpThruType;
            if (!self.IgnoreJumpThrus && udjtType != null)
                return self.CollideCheckOutside(udjtType, self.Position - Vector2.UnitY * downCheck, true);

            return false;
        }

        private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, player);
                return;
            }

            // TODO: not copy the entire contents of Level.EnforceBounds

            Rectangle bounds = self.Bounds;
            Rectangle rectangle = new Rectangle((int) self.Camera.Left, (int) self.Camera.Top, 320, 180);
            if (self.Transitioning)
                return;
            if (self.CameraLockMode == Level.CameraLockModes.FinalBoss && player.Left < (double) rectangle.Left)
            {
                player.Left = rectangle.Left;
                player.OnBoundsH();
            }
            else if (player.Left < (double) bounds.Left)
            {
                if (player.Top >= (double) bounds.Top && player.Bottom < (double) bounds.Bottom &&
                    self.Session.MapData.CanTransitionTo(self, player.Center + Vector2.UnitX * -8f))
                {
                    player.BeforeSideTransition();
                    self.CallNextLevel(player.Center + Vector2.UnitX * -8f, -Vector2.UnitX);
                    return;
                }

                player.Left = bounds.Left;
                player.OnBoundsH();
            }

            TheoCrystal entity = self.Tracker.GetEntity<TheoCrystal>();
            if (self.CameraLockMode == Level.CameraLockModes.FinalBoss && player.Right > (double) rectangle.Right &&
                rectangle.Right < bounds.Right - 4)
            {
                player.Right = rectangle.Right;
                player.OnBoundsH();
            }
            else if (entity != null && (player.Holding == null || !player.Holding.IsHeld) && player.Right > (double) (bounds.Right - 1))
                player.Right = bounds.Right - 1;
            else if (player.Right > (double) bounds.Right)
            {
                if (player.Top >= (double) bounds.Top && player.Bottom < (double) bounds.Bottom &&
                    self.Session.MapData.CanTransitionTo(self, player.Center + Vector2.UnitX * 8f))
                {
                    player.BeforeSideTransition();
                    self.CallNextLevel(player.Center + Vector2.UnitX * 8f, Vector2.UnitX);
                    return;
                }

                player.Right = bounds.Right;
                player.OnBoundsH();
            }

            // changes start here

            void tryToDie(int bounceAtPoint)
            {
                if (SaveData.Instance.Assists.Invincible)
                {
                    player.Play("event:/game/general/assist_screenbottom");
                    player.Bounce(bounceAtPoint);
                }
                else
                    player.Die(Vector2.Zero);
            }

            // transition down if required
            if (self.CameraLockMode != Level.CameraLockModes.None && player.Bottom > rectangle.Bottom)
            {
                player.Bottom = rectangle.Bottom;
                player.OnBoundsV();
            }
            else if (player.CenterY > bounds.Bottom)
            {
                if (self.Session.MapData.CanTransitionTo(self,
                        player.Center + Vector2.UnitY * 12f) &&
                    !self.Session.LevelData.DisableDownTransition &&
                    !player.CollideCheck<Solid>(player.Position + Vector2.UnitY * 4f))
                {
                    player.BeforeDownTransition();
                    self.CallNextLevel(player.Center + Vector2.UnitY * 12f, Vector2.UnitY);
                }
                else
                {
                    player.CenterY = bounds.Bottom;
                    player.OnBoundsV();
                }
            }

            // die or transition up if required
            if (self.CameraLockMode != Level.CameraLockModes.None && rectangle.Top > bounds.Top + 4 && player.Bottom < rectangle.Top)
                tryToDie(rectangle.Top);
            else if (player.Top < bounds.Top && self.Session.MapData.CanTransitionTo(self, player.Center - Vector2.UnitY * 12f))
            {
                player.BeforeUpTransition();
                self.CallNextLevel(player.Center - Vector2.UnitY * 12f, -Vector2.UnitY);
            }
            else if (player.Bottom < bounds.Top - 4)
                player.Die(Vector2.Zero);
        }

        private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (GravityHelperModule.Settings.ToggleInvertGravity.Pressed)
            {
                GravityHelperModule.Settings.ToggleInvertGravity.ConsumePress();
                GravityHelperModule.Instance.Gravity = GravityType.Toggle;
            }

            orig(self);
        }

        private static void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, move);
                return;
            }

            GravityHelperModule.SolidMoving = true;
            orig(self, move);
            GravityHelperModule.SolidMoving = false;
        }

        private static int SolidTiles_GetLandSoundIndex(On.Celeste.SolidTiles.orig_GetLandSoundIndex orig, SolidTiles self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static int SolidTiles_GetStepSoundIndex(On.Celeste.SolidTiles.orig_GetStepSoundIndex orig, SolidTiles self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static void Spikes_ctor_Vector2_int_Directions_string(On.Celeste.Spikes.orig_ctor_Vector2_int_Directions_string orig, Spikes self, Vector2 position, int size, Spikes.Directions direction, string type)
        {
            orig(self, position, size, direction, type);

            // we add a disabled ledge blocker for downward spikes
            if (self.Direction == Spikes.Directions.Down)
                self.Add(new LedgeBlocker { Blocking = false });

            self.Add(new GravityListener());
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

        private static void Spring_OnCollide(On.Celeste.Spring.orig_OnCollide orig, Spring self, Player player)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, player);
                return;
            }

            // check copied from orig
            if (player.StateMachine.State == Player.StDreamDash || !self.GetPlayerCanUse())
                return;

            // if we hit a floor spring while inverted, flip gravity back to normal
            if (self.Orientation == Spring.Orientations.Floor)
                GravityHelperModule.Instance.Gravity = GravityType.Normal;

            orig(self, player);
        }

        #endregion
    }
}