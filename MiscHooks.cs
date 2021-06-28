using System;
using System.Runtime.CompilerServices;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper
{
    public static class MiscHooks
    {
        private static IDetour hook_Level_orig_TransitionRoutine;
        private static IDetour hook_PlayerDeadBody_DeathRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading miscellaneous hooks...");

            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding_JumpThru;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding_Solid;
            IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            IL.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
            IL.Celeste.PlayerDeadBody.Render += PlayerDeadBody_Render;
            IL.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
            IL.Celeste.PlayerHair.Render += PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;

            On.Celeste.Actor.MoveV += Actor_MoveV;
            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
            On.Celeste.Level.Update += Level_Update;
            On.Celeste.JumpThru.HasPlayerRider += JumpThru_HasPlayerRider;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
            On.Celeste.SolidTiles.GetLandSoundIndex += SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex += SolidTiles_GetStepSoundIndex;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide += Spikes_OnCollide;
            On.Celeste.Spring.OnCollide += Spring_OnCollide;
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool += TrailManager_Add;

            hook_Level_orig_TransitionRoutine = new ILHook(ReflectionCache.Level_OrigTransitionRoutine.GetStateMachineTarget(), Level_orig_TransitionRoutine);
            hook_PlayerDeadBody_DeathRoutine = new ILHook(ReflectionCache.PlayerDeadBody_DeathRoutine.GetStateMachineTarget(), PlayerDeadBody_DeathRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading miscellaneous hooks...");

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding_JumpThru;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding_Solid;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            IL.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
            IL.Celeste.PlayerDeadBody.Render -= PlayerDeadBody_Render;
            IL.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
            IL.Celeste.PlayerHair.Render -= PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;

            On.Celeste.Actor.MoveV -= Actor_MoveV;
            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
            On.Celeste.Level.Update -= Level_Update;
            On.Celeste.JumpThru.HasPlayerRider -= JumpThru_HasPlayerRider;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
            On.Celeste.SolidTiles.GetLandSoundIndex -= SolidTiles_GetLandSoundIndex;
            On.Celeste.SolidTiles.GetStepSoundIndex -= SolidTiles_GetStepSoundIndex;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string -= Spikes_ctor_Vector2_int_Directions_string;
            On.Celeste.Spikes.OnCollide -= Spikes_OnCollide;
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool -= TrailManager_Add;

            hook_Level_orig_TransitionRoutine?.Dispose();
            hook_Level_orig_TransitionRoutine = null;

            hook_PlayerDeadBody_DeathRoutine?.Dispose();
            hook_PlayerDeadBody_DeathRoutine = null;
        }

        #region IL Hooks

        private static void Actor_IsRiding_JumpThru(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(1),
                instr => instr.MatchLdarg(0)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<Actor, JumpThru, bool>>((self, jumpThru) =>
                    GravityHelperModule.ShouldInvert && jumpThru is UpsideDownJumpThru && self is Player &&
                    self.CollideCheckOutside(jumpThru, self.Position - Vector2.UnitY) ||
                    !GravityHelperModule.ShouldInvert && jumpThru is not UpsideDownJumpThru &&
                    self.CollideCheckOutside(jumpThru, self.Position + Vector2.UnitY));
                cursor.Emit(OpCodes.Ret);
            }
            else
            {
                throw new Exception("Couldn't patch Actor.IsRiding for jumpthrus");
            }
        });

        private static void Actor_IsRiding_Solid(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY")))
                cursor.EmitInvertVectorDelegate();
            else
                throw new Exception("Couldn't patch Actor.IsRiding for solids");
        });

        private static void Actor_MoveVExact(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1),
                instr => instr.MatchLdcI4(0) && instr.Next.MatchBle(out _)))
            {
                cursor.Next.MatchBle(out var label);
                cursor.Remove();
                cursor.Emit(OpCodes.Beq_S, label);
            }
            else
            {
                throw new Exception("Couldn't patch ble to beq.");
            }

            if (cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideFirstOutside), out _)))
            {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldloc_1); // num1
                cursor.Emit(OpCodes.Ldarg_1); // moveV
                cursor.EmitDelegate<Func<Actor, Vector2, int, int, JumpThru>>((self, at, num1, moveV) =>
                    moveV > 0
                        ? self.CollideFirstOutside<JumpThru>(at)
                        : self.CollideFirstOutside<UpsideDownJumpThru>(self.Position + Vector2.UnitY * num1));
            }
            else
            {
                throw new Exception("Couldn't replace CollideFirstOutside<JumpThru>.");
            }
        });

        private static void Bumper_OnPlayer(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
            {
                if (!GravityHelperModule.ShouldInvert) return v;
                return new Vector2(v.X, p.CenterY - (v.Y - p.CenterY));
            });
        });

        private static void Level_orig_TransitionRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            //// if (direction == Vector2.UnitY)
            cursor.GotoNext(instr =>
                instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Equality"));
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
        });

        private static void PlayerDeadBody_DeathRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // playerDeadBody1.deathEffect = new DeathEffect(playerDeadBody1.initialHairColor, new Vector2?(playerDeadBody1.Center - playerDeadBody1.Position));
            cursor.GotoNext(instr => instr.MatchLdfld<PlayerDeadBody>("initialHairColor"));
            cursor.GotoNextSubtraction(MoveType.After);
            cursor.EmitInvertVectorDelegate();

            // playerDeadBody1.Position = playerDeadBody1.Position + Vector2.UnitY * -5f;
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-5));
            cursor.EmitInvertFloatDelegate();
        });

        private static void PlayerDeadBody_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.sprite.Scale.Y = this.scale;
            cursor.GotoNext(instr => instr.MatchStfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
        });

        private static void PlayerHair_AfterUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            void invertAdditions()
            {
                cursor.GotoNextAddition();
                cursor.EmitInvertVectorDelegate();
                cursor.Index += 2;
                cursor.GotoNextAddition();
                cursor.EmitInvertVectorDelegate();
            }

            // this.Nodes[0] = this.Sprite.RenderPosition + new Vector2(0.0f, -9f * this.Sprite.Scale.Y) + this.Sprite.HairOffset * new Vector2((float) this.Facing, 1f);
            invertAdditions();

            // Vector2 target = this.Nodes[0] + new Vector2((float) ((double) -(int) this.Facing * (double) this.StepInFacingPerSegment * 2.0), (float) Math.Sin((double) this.wave) * this.StepYSinePerSegment) + this.StepPerSegment;
            cursor.GotoNext(instr => instr.MatchLdfld<PlayerHair>(nameof(PlayerHair.StepYSinePerSegment)));
            invertAdditions();

            // target = this.Nodes[index] + new Vector2((float) -(int) this.Facing * this.StepInFacingPerSegment, (float) Math.Sin((double) this.wave + (double) index * 0.800000011920929) * this.StepYSinePerSegment) + this.StepPerSegment;
            cursor.GotoNext(instr => instr.MatchLdfld<PlayerHair>(nameof(PlayerHair.StepYSinePerSegment)));
            invertAdditions();
        });

        private static void PlayerHair_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Vector2 hairScale = this.GetHairScale(index);
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
            cursor.EmitInvertVectorDelegate();

            // this.GetHairTexture(index).Draw(this.Nodes[index], origin, this.GetHairColor(index), this.GetHairScale(index));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
            cursor.EmitInvertVectorDelegate();
        });

        private static void Solid_GetPlayerOnTop(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNextSubtraction();
            cursor.EmitInvertVectorDelegate();
        });

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
            if (!GravityHelperModule.ShouldInvert || self is not Player)
                return orig(self, downCheck);

            if (self.CollideCheck<Solid>(self.Position - Vector2.UnitY * downCheck))
                return true;

            if (!self.IgnoreJumpThrus)
                return self.CollideCheckOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY * downCheck);

            return false;
        }

        private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, player);
                return;
            }

            // horizontal code copied from vanilla
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
            if (self.CameraLockMode == Level.CameraLockModes.FinalBoss && player.Right > (double) rectangle.Right && rectangle.Right < bounds.Right - 4)
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

            // custom vertical code
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
                else if (player.Bottom > bounds.Bottom + 24)
                {
                    player.Bottom = bounds.Bottom + 24;
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
            else if (player.Bottom < bounds.Top && SaveData.Instance.Assists.Invincible)
                tryToDie(bounds.Top);
            else if (player.Bottom < bounds.Top - 4)
                player.Die(Vector2.Zero);
        }

        private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (GravityHelperModule.Settings.ToggleInvertGravity.Pressed)
            {
                GravityHelperModule.Settings.ToggleInvertGravity.ConsumePress();
                GravityHelperModule.Instance.SetGravity(GravityType.Toggle);
            }

            orig(self);
        }

        private static bool JumpThru_HasPlayerRider(On.Celeste.JumpThru.orig_HasPlayerRider orig, JumpThru self) =>
            GravityHelperModule.ShouldInvert == self is UpsideDownJumpThru && orig(self);

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
                self.Add(new LedgeBlocker {Blocking = false});

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
                GravityHelperModule.Instance.SetGravity(GravityType.Normal);

            orig(self, player);
        }

        private static TrailManager.Snapshot TrailManager_Add(
            On.Celeste.TrailManager.orig_Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool orig,
            Vector2 position,
            Image sprite,
            PlayerHair hair,
            Vector2 scale,
            Color color,
            int depth,
            float duration,
            bool frozenUpdate,
            bool useRawDeltaTime)
        {
            if (GravityHelperModule.ShouldInvert)
                scale = new Vector2(scale.X, -scale.Y);

            return orig(position, sprite, hair, scale, color, depth, duration, frozenUpdate, useRawDeltaTime);
        }

        #endregion
    }
}
