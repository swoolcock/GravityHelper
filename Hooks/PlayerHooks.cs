// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class PlayerHooks
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_Player_DashCoroutine;
        private static IDetour hook_Player_orig_Update;
        private static IDetour hook_Player_orig_UpdateSprite;
        private static IDetour hook_Player_orig_WallJump;
        private static IDetour hook_Player_ctor_OnFrameChange;
        private static IDetour hook_Player_get_CanUnDuck;
        // ReSharper restore InconsistentNaming

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Player)} hooks...");

            IL.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;
            IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
            IL.Celeste.Player.Bounce += Player_Bounce;
            IL.Celeste.Player.ClimbHopBlockedCheck += Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbJump += Player_ClimbJump;
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.CreateWallSlideParticles += Player_CreateWallSlideParticles;
            IL.Celeste.Player.DashUpdate += Player_DashUpdate;
            IL.Celeste.Player._IsOverWater += Player_IsOverWater;
            IL.Celeste.Player.Jump += Player_Jump;
            IL.Celeste.Player.LaunchedBoostCheck += Player_LaunchedBoostCheck;
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            IL.Celeste.Player.OnCollideH += Player_OnCollideH;
            IL.Celeste.Player.OnCollideV += Player_OnCollideV;
            IL.Celeste.Player.PointBounce += Player_PointBounce;
            IL.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            IL.Celeste.Player.SideBounce += Player_SideBounce;
            IL.Celeste.Player.SlipCheck += Player_SlipCheck;
            IL.Celeste.Player.StarFlyUpdate += Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce += Player_SuperBounce;
            IL.Celeste.Player.SuperJump += Player_SuperJump;
            IL.Celeste.Player.SuperWallJump += Player_SuperWallJump;
            IL.Celeste.Player.SwimCheck += Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck += Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck += Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck += Player_SwimUnderwaterCheck;
            IL.Celeste.Player.UpdateCarry += Player_UpdateCarry;

            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
            On.Celeste.Player.CassetteFlyEnd += Player_CassetteFlyEnd;
            On.Celeste.Player.CreateTrail += Player_CreateTrail;
            On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.DustParticleFromSurfaceIndex += Player_DustParticleFromSurfaceIndex;
            On.Celeste.Player.JumpThruBoostBlockedCheck += Player_JumpThruBoostBlockedCheck;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.ReflectBounce += Player_ReflectBounce;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.StarFlyBegin += Player_StarFlyBegin;
            On.Celeste.Player.StartCassetteFly += Player_StartCassetteFly;
            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.WindMove += Player_WindMove;

            using (new DetourContext { Before = { "MaxHelpingHand", "SpringCollab2020" }})
                hook_Player_orig_Update = new ILHook(ReflectionCache.Player_OrigUpdate, Player_orig_Update);

            hook_Player_DashCoroutine = new ILHook(ReflectionCache.Player_DashCoroutine.GetStateMachineTarget(), Player_DashCoroutine);
            hook_Player_orig_UpdateSprite = new ILHook(ReflectionCache.Player_OrigUpdateSprite, Player_orig_UpdateSprite);
            hook_Player_orig_WallJump = new ILHook(ReflectionCache.Player_OrigWallJump, Player_orig_WallJump);
            hook_Player_get_CanUnDuck = new ILHook(ReflectionCache.Player_CanUnDuck, Player_get_CanUnDuck);

            // we assume the first .ctor method that accepts (string) is Sprite.OnFrameChange +=
            var spriteOnFrameChange = typeof(Player).GetRuntimeMethods().FirstOrDefault(m =>
            {
                if (!m.Name.Contains(".ctor")) return false;
                var parameters = m.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
            });

            if (spriteOnFrameChange != null)
                hook_Player_ctor_OnFrameChange = new ILHook(spriteOnFrameChange, Player_ctor_OnFrameChange);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Player)} hooks...");

            IL.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;
            IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
            IL.Celeste.Player.Bounce -= Player_Bounce;
            IL.Celeste.Player.ClimbHopBlockedCheck -= Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbJump -= Player_ClimbJump;
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.CreateWallSlideParticles -= Player_CreateWallSlideParticles;
            IL.Celeste.Player.DashUpdate -= Player_DashUpdate;
            IL.Celeste.Player._IsOverWater -= Player_IsOverWater;
            IL.Celeste.Player.Jump -= Player_Jump;
            IL.Celeste.Player.LaunchedBoostCheck -= Player_LaunchedBoostCheck;
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            IL.Celeste.Player.OnCollideH -= Player_OnCollideH;
            IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
            IL.Celeste.Player.PointBounce -= Player_PointBounce;
            IL.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
            IL.Celeste.Player.SideBounce -= Player_SideBounce;
            IL.Celeste.Player.SlipCheck -= Player_SlipCheck;
            IL.Celeste.Player.StarFlyUpdate -= Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce -= Player_SuperBounce;
            IL.Celeste.Player.SuperJump -= Player_SuperJump;
            IL.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
            IL.Celeste.Player.SwimCheck -= Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck -= Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck -= Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck -= Player_SwimUnderwaterCheck;
            IL.Celeste.Player.UpdateCarry -= Player_UpdateCarry;

            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.CassetteFlyEnd -= Player_CassetteFlyEnd;
            On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
            On.Celeste.Player.CreateTrail -= Player_CreateTrail;
            On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.DustParticleFromSurfaceIndex -= Player_DustParticleFromSurfaceIndex;
            On.Celeste.Player.JumpThruBoostBlockedCheck -= Player_JumpThruBoostBlockedCheck;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.ReflectBounce -= Player_ReflectBounce;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.StarFlyBegin -= Player_StarFlyBegin;
            On.Celeste.Player.StartCassetteFly -= Player_StartCassetteFly;
            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.WindMove -= Player_WindMove;

            hook_Player_DashCoroutine?.Dispose();
            hook_Player_DashCoroutine = null;

            hook_Player_orig_Update?.Dispose();
            hook_Player_orig_Update = null;

            hook_Player_orig_UpdateSprite?.Dispose();
            hook_Player_orig_UpdateSprite = null;

            hook_Player_orig_WallJump?.Dispose();
            hook_Player_orig_WallJump = null;

            hook_Player_ctor_OnFrameChange?.Dispose();
            hook_Player_ctor_OnFrameChange = null;

            hook_Player_get_CanUnDuck?.Dispose();
            hook_Player_get_CanUnDuck = null;
        }

        #region IL Hooks

        private static void Player_BeforeDownTransition(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            var target = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(self =>
            {
                if (!GravityHelperModule.ShouldInvertPlayer)
                    return false;

                // copied from Player.BeforeUpTransition
                self.Speed.X = 0.0f;
                if (self.StateMachine.State != Player.StRedDash && self.StateMachine.State != Player.StReflectionFall &&
                    self.StateMachine.State != Player.StStarFly)
                {
                    self.SetVarJumpSpeed(self.Speed.Y = -105f);
                    self.StateMachine.State = self.StateMachine.State != Player.StSummitLaunch
                        ? Player.StNormal
                        : Player.StIntroJump;
                    self.AutoJump = true;
                    self.AutoJumpTimer = 0.0f;
                    self.SetVarJumpTimer(0.2f);
                }

                self.SetDashCooldownTimer(0.2f);

                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, target);
            cursor.Emit(OpCodes.Ret);
        });

        private static void Player_BeforeUpTransition(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            var target = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(self =>
            {
                if (!GravityHelperModule.ShouldInvertPlayer)
                    return false;

                // copied from Player.BeforeDownTransition
                if (self.StateMachine.State != Player.StRedDash && self.StateMachine.State != Player.StReflectionFall &&
                    self.StateMachine.State != Player.StStarFly)
                {
                    self.StateMachine.State = Player.StNormal;
                    self.Speed.Y = Math.Max(0.0f, self.Speed.Y);
                    self.AutoJump = false;
                    self.SetVarJumpTimer(0.0f);
                }

                foreach (Entity entity in self.Scene.Tracker.GetEntities<Platform>())
                {
                    if (!(entity is SolidTiles) &&
                        self.CollideCheckOutside(entity, self.Position - Vector2.UnitY * self.Height))
                        entity.Collidable = false;
                }

                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, target);
            cursor.Emit(OpCodes.Ret);
        });

        private static void Player_Bounce(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.BottomPredicate))
                throw new HookException("Couldn't find Bottom.");

            cursor.EmitInvertEntityPoint(nameof(Entity.Bottom));

            if (!cursor.TryGotoNext(instr => instr.MatchLdnull(), instr => instr.MatchLdnull()))
                throw new HookException("Couldn't find ldnull/ldnull");

            cursor.EmitInvertIntDelegate();
        });

        private static void Player_ClimbHopBlockedCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(6));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_ClimbJump(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Dust.Burst(this.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchLdstr("event:/char/madeline/jump_climb_right"));
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();

            // Dust.Burst(this.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchLdstr("event:/char/madeline/jump_climb_left"));
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_ClimbUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // replace all calls to LiftBoost (should be 4)
            cursor.replaceGetLiftBoost(4);
            cursor.Goto(0);

            // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
            cursor.GotoNext(MoveType.After,
                instr => ILCursorExtensions.UnitYPredicate(instr) && ILCursorExtensions.SubtractionPredicate(instr.Next));
            cursor.EmitInvertVectorDelegate();

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            cursor.GotoNextAddition();
            cursor.EmitInvertVectorDelegate();

            // borrowed from MaxHelpingHand
            if (cursor.TryGotoNext(instr => instr.MatchStfld<Vector2>("Y"),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Player>("climbNoMoveTimer"),
                instr => instr.MatchLdcR4(0.0f)))
            {
                cursor.Index += 2;
                FieldInfo field = typeof (Player).GetField("lastClimbMove", BindingFlags.Instance | BindingFlags.NonPublic);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, field);
                cursor.EmitDelegate<Func<Player, int, int>>(updateClimbMove);
                cursor.Emit(OpCodes.Stfld, field);
                cursor.Emit(OpCodes.Ldarg_0);
            }
        });

        private static int updateClimbMove(Player player, int lastClimbMove)
        {
            if (Input.MoveY.Value != -1)
                return lastClimbMove;

            if (!GravityHelperModule.ShouldInvertPlayer &&
                !player.CollideCheckOutsideUpsideDownJumpThru(player.Position - Vector2.UnitY))
                return lastClimbMove;

            if (GravityHelperModule.ShouldInvertPlayer &&
                !player.CollideCheckOutsideNotUpsideDownJumpThru(player.Position + Vector2.UnitY))
                return lastClimbMove;

            player.Speed.Y = 0.0f;
            return 0;
        }

        private static void Player_CreateWallSlideParticles(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Dust.Burst(dir != 1 ? center + new Vector2(-x, 4f) : center + new Vector2(x, 4f), -1.5707964f, particleType: particleType);

            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(4) && instr.Next.MatchNewobj<Vector2>()))
                throw new HookException("Couldn't match first instance of 4f");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(4) && instr.Next.MatchNewobj<Vector2>()))
                throw new HookException("Couldn't match second instance of 4f");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(instr => instr.MatchLdcI4(1) && instr.Previous.MatchLdcR4(out _)))
                throw new HookException("Couldn't match -PI/2f");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_ctor_OnFrameChange(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Dust.BurstFG(this.Position + new Vector2((float) (-(int) this.Facing * 5), -1f), new Vector2((float) -(int) this.Facing, -0.5f).Angle(), range: 0.0f);
            cursor.GotoNext(instr => instr.MatchLdstr("push"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-1f));
            cursor.EmitInvertFloatDelegate();
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-0.5f));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_DashCoroutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            /*
             * if (player.onGround && (double) player.DashDir.X != 0.0 && ((double) player.DashDir.Y > 0.0 && (double) player.Speed.Y > 0.0) && (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
             */
            // Ensure dream block check is the correct direction, otherwise we can't hyper on top of dream blocks
            cursor.GotoNextAddition();
            cursor.EmitInvertVectorDelegate();

            /*
             * SlashFx.Burst(player.Center, player.DashDir.Angle());
             */
            // Fix dash effect direction
            cursor.GotoNext(instr => instr.MatchCall<SlashFx>(nameof(SlashFx.Burst)));
            cursor.Index--;
            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_DashUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            emitDashUpdateFixes(il);
        });

        private static void Player_get_CanUnDuck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _)))
                throw new HookException("Couldn't hook jumpthru checks in Player.CanUnDuck");

            cursor.Remove();
            cursor.EmitDelegate<Func<Player, bool>>(self =>
                self.CollideCheck<Solid>() ||
                !GravityHelperModule.ShouldInvertPlayer && self.CollideCheckUpsideDownJumpThru() ||
                GravityHelperModule.ShouldInvertPlayer && self.CollideCheck<JumpThru>());
        });

        private static void Player_IsOverWater(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdloc(0));
            cursor.EmitDelegate<Func<Rectangle, Rectangle>>(r =>
            {
                if (GravityHelperModule.ShouldInvertPlayer) r.Y -= 2;
                return r;
            });
        });

        private static void Player_Jump(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            if (!cursor.TryGotoNext(instr => instr.MatchCall<Vector2>("get_UnitY")))
                throw new HookException("Couldn't find UnitY");

            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
            if (!cursor.TryGotoNext(ILCursorExtensions.BottomCenterPredicate))
                throw new HookException("Couldn't find BottomCenter");

            cursor.EmitInvertEntityPoint(nameof(Entity.BottomCenter));

            if (!cursor.TryGotoNext(instr => instr.MatchLdcI4(4)))
                throw new HookException("Couldn't find ldci4(4)");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_LaunchedBoostCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();
        });

        private static void Player_NormalUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            cursor.replaceGetLiftBoost(3);
            cursor.Goto(0);

            // if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float) -index) && this.ClimbCheck((int) this.Facing, -index))
            cursor.GotoNextUnitY(MoveType.After);
            cursor.EmitInvertVectorDelegate();

            // if ((water = this.CollideFirst<Water>(this.Position + Vector2.UnitY * 2f)) != null)
            cursor.GotoNextUnitY(MoveType.After);
            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_OnCollideH(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Vector2 add = new Vector2((float) Math.Sign(this.Speed.X), (float) (index1 * index2));
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchConvR4(),
                instr => instr.MatchLdloc(out _),
                instr => instr.MatchLdloc(out _),
                instr => instr.MatchMul(),
                instr => instr.MatchConvR4()))
                throw new HookException("Couldn't invert (index1 * index2)");

            cursor.EmitInvertFloatDelegate();

            // if (!this.CollideCheck<Solid>(at) && this.CollideCheck<Solid>(at - Vector2.UnitY * (float) index2) && !this.DashCorrectCheck(add))
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY")))
                throw new HookException("Couldn't invert call to CollideCheck<Solid>");

            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_OnCollideV(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>("get_DashAttacking"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall("System.Math", nameof(Math.Sign)));
            cursor.EmitInvertIntDelegate();

            // this.ReflectBounce(new Vector2(0.0f, (float) -Math.Sign(this.Speed.Y)));
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ReflectBounce)));
            cursor.EmitInvertVectorDelegate();

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + new Vector2(0.0f, 1f), this.temp));
            cursor.GotoNext(instr => instr.MatchCall<SurfaceIndex>(nameof(SurfaceIndex.GetPlatformByPriority)));
            cursor.GotoPrev(ILCursorExtensions.AdditionPredicate);
            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.Position, new Vector2(0.0f, -1f).Angle(), 8, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>("DustParticleFromSurfaceIndex"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-1));
            cursor.EmitInvertFloatDelegate();

            // ceiling pop correction
            for (int i = 0; i < 4; i++)
            {
                cursor.GotoNext(instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));
                cursor.GotoNext(ILCursorExtensions.AdditionPredicate);
                cursor.EmitInvertVectorDelegate();
                cursor.Index += 2;
            }
        });

        private static void Player_orig_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            /*
             * else if ((double) this.Speed.Y >= 0.0)
             * {
             *   Platform platform = (Platform) this.CollideFirst<Solid>(this.Position + Vector2.UnitY) ?? (Platform) this.CollideFirstOutside<JumpThru>(this.Position + Vector2.UnitY);
             *   if (platform != null)
             */

            // ensure we check ground collisions the right direction
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY")))
                throw new HookException("Couldn't apply patch to check ground collisions.");

            cursor.EmitInvertVectorDelegate();

            // prevent Madeline from attempting to stand on the underside of regular jumpthrus
            // or the topside of upside down jumpthrus
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate) ||
                !cursor.TryGotoNext(instr => instr.MatchLdloc(1), instr => instr.MatchBrfalse(out _)))
                throw new HookException("Couldn't find platform != null check.");

            var platformNotEqualNull = cursor.Next;
            if (!cursor.TryGotoPrev(instr => instr.MatchLdarg(0), instr => instr.MatchLdarg(0)))
                throw new HookException("Couldn't apply patch for jumpthrus.");

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<Player, Platform>>(self =>
                !GravityHelperModule.ShouldInvertPlayer
                    ? self.CollideFirstOutside<JumpThru>(self.Position + Vector2.UnitY)
                    : self.CollideFirstOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY));
            cursor.Emit(OpCodes.Stloc_1);
            cursor.Emit(OpCodes.Br_S, platformNotEqualNull);
            cursor.Goto(platformNotEqualNull);

            /*
             * else if (this.onGround && (this.CollideCheck<Solid, NegaBlock>(this.Position + Vector2.UnitY) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY)) && (!this.CollideCheck<Spikes>(this.Position) || SaveData.Instance.Assists.Invincible))
             */

            // ensure we check ground collisions the right direction for refilling dash on solid ground
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't apply patch for dash refill.");

            cursor.EmitInvertVectorDelegate();

            // find some instructions
            if (!cursor.TryGotoNext(instr => instr.Match(OpCodes.Ldarg_0),
                instr => instr.Match(OpCodes.Ldarg_0)))
                throw new HookException("Couldn't find jumpthru check.");

            var jumpThruCheck = cursor.Next;

            if (!cursor.TryGotoNext(MoveType.After, ILCursorExtensions.AdditionPredicate) ||
                !cursor.TryGotoNext(instr => instr.MatchLdarg(0)))
                throw new HookException("Couldn't find spikes check.");

            var spikesCheck = cursor.Next;

            if (!cursor.TryGotoNext(instr => instr.Match(OpCodes.Ldarg_0),
                instr => instr.MatchLdfld<Player>("varJumpTimer")))
                throw new HookException("Couldn't find varJumpTimer check");

            var varJumpTimerCheck = cursor.Next;

            // replace the JumpThru check with UpsideDownJumpThru if we can and should
            cursor.Goto(jumpThruCheck);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<Player, bool>>(self =>
                !GravityHelperModule.ShouldInvertPlayer
                    ? self.CollideCheckOutside<JumpThru>(self.Position + Vector2.UnitY)
                    : self.CollideCheckOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY));
            cursor.Emit(OpCodes.Brfalse_S, varJumpTimerCheck);
            cursor.Emit(OpCodes.Br_S, spikesCheck);

            /*
             * else if (climbHopSolid != null && climbHopSolid.Position != climbHopSolidPosition)
	         * {
		     *     Vector2 vector = climbHopSolid.Position - climbHopSolidPosition;
		     *     climbHopSolidPosition = climbHopSolid.Position;
		     *     MoveHExact((int)vector.X);
		     *     MoveVExact((int)vector.Y);
	         * }
             */
            // ensure we correctly hop on the underside of moving blocks (kevins, etc.)
            if (!cursor.TryGotoNext(instr => instr.MatchLdfld<Player>("climbHopSolid")) ||
                !cursor.TryGotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveVExact))) ||
                !cursor.TryGotoPrev(MoveType.After, instr => instr.MatchConvI4()))
                throw new HookException("Couldn't apply moving block check.");

            cursor.EmitInvertIntDelegate();

            // change speedring angle
            if (!cursor.TryGotoNext(instr => instr.MatchCall<Engine>("get_Pooler")) ||
                !cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Player>(nameof(Player.Speed))))
                throw new HookException("Couldn't find Engine.Pooler.");

            cursor.EmitInvertVectorDelegate();

            // skip to base.Update();
            if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchCall<Actor>(nameof(Actor.Update))))
                throw new HookException("Couldn't find base.Update()");

            // find collidecheck
            if (!cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity, JumpThru>(nameof(Entity.CollideCheck), out _)))
                throw new HookException("Couldn't find CollideCheck<JumpThru>");

            // emit UDJT check
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Func<Entity, bool>>(e => e.CollideCheckUpsideDownJumpThru());
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // find 3
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(3)))
                throw new HookException("Couldn't find 3 (1).");

            // invert solid dash correction check
            cursor.EmitInvertFloatDelegate();

            // find 3
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(3)))
                throw new HookException("Couldn't find 3 (2).");

            // invert jumpthru dash correction check
            cursor.EmitInvertFloatDelegate();

            // find collidecheckoutside
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallGeneric<Entity, JumpThru>(nameof(Entity.CollideCheckOutside), out _)))
                throw new HookException("Couldn't find CollideCheckOutside<JumpThru>");

            // emit UDJT check AFTER, to be compatible with MHH's hooks
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, Player, bool>>((b, self) =>
                !GravityHelperModule.ShouldInvertPlayer
                    ? b
                    : self.CollideCheckOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY * 3f));

            // find 3
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(3)))
                throw new HookException("Couldn't find 3 (3).");

            // invert dash correct check
            cursor.EmitInvertFloatDelegate();

            /*
             * if (water != null && (double) this.Center.Y < (double) water.Center.Y)
             */

            // invert Center.Y check (fixes Madeline slamming into the ground when climbing down into water)
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck")) ||
                !cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck")) ||
                !cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck")))
                throw new HookException("Couldn't skip three SwimChecks.");

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find first Vector2.Y");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't find second Vector2.Y");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_orig_UpdateSprite(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // fix dangling animation
            cursor.GotoNext(instr => instr.MatchLdstr("dangling"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(6));
            cursor.EmitInvertFloatDelegate();

            cursor.GotoNext(instr => instr.MatchLdstr("idle_carry"));
            for (int i = 0; i < 2; i++)
            {
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(2));
                cursor.EmitInvertFloatDelegate();
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(2));
                cursor.EmitInvertFloatDelegate();
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(2));
                cursor.EmitInvertFloatDelegate();

                cursor.GotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _));
                cursor.Remove();
                cursor.EmitDelegate<Func<Player, Vector2, bool>>((self, at) =>
                    !GravityHelperModule.ShouldInvertPlayer
                        ? self.CollideCheck<JumpThru>(at)
                        : self.CollideCheckUpsideDownJumpThru(at));
            }
        });

        private static void Player_orig_WallJump(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();
        });

        private static void Player_PointBounce(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, ILCursorExtensions.SubtractionPredicate))
                throw new HookException("Couldn't invert bounce direction.");

            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_RedDashUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            emitDashUpdateFixes(il);
        });

        private static void Player_SideBounce(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.MoveV(Calc.Clamp(fromY - this.Bottom, -4f, 4f));
            if (!cursor.TryGotoNext(ILCursorExtensions.BottomPredicate))
                throw new HookException("Couldn't find Bottom");

            cursor.EmitInvertEntityPoint(nameof(Entity.Bottom));

            if (!cursor.TryGotoNext(instr => instr.MatchLdnull(), instr => instr.MatchLdnull()))
                throw new HookException("Couldn't find ldnull/ldnull");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SlipCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(instr => instr.MatchCall<Entity>("get_TopRight")))
                throw new HookException("Couldn't replace TopRight with BottomRight while inverted");
            cursor.EmitInvertEntityPoint(nameof(Entity.TopRight));

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
                throw new HookException("Couldn't replace 4 with 5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvertPlayer ? f + 1 : f);

            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't replace vector addition with subtraction");
            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(instr => instr.MatchCall<Entity>("get_TopLeft")))
                throw new HookException("Couldn't replace TopLeft with BottomLeft while inverted");
            cursor.EmitInvertEntityPoint(nameof(Entity.TopLeft));

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
                throw new HookException("Couldn't replace 4 with 5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvertPlayer ? f + 1 : f);

            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't replace vector addition with subtraction");
            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-4)))
                throw new HookException("Couldn't replace -4 with -5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvertPlayer ? f - 1 : f);

            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't replace vector addition with subtraction");
            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_StarFlyUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.level.Particles.Emit(FlyFeather.P_Flying, 1, this.Center, Vector2.One * 2f, (-this.Speed).Angle());
            cursor.GotoNext(instr => instr.MatchCallvirt<ParticleSystem>(nameof(ParticleSystem.Emit)));
            cursor.Index--;
            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_SuperBounce(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.MoveV(fromY - this.Bottom);
            if (!cursor.TryGotoNext(ILCursorExtensions.BottomPredicate))
                throw new HookException("Couldn't find Bottom");

            cursor.EmitInvertEntityPoint(nameof(Entity.Bottom));

            if (!cursor.TryGotoNext(instr => instr.MatchLdnull(), instr => instr.MatchLdnull()))
                throw new HookException("Couldn't find ldnull/ldnull");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SuperJump(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            cursor.GotoNext(instr => instr.MatchCall<SurfaceIndex>(nameof(SurfaceIndex.GetPlatformByPriority)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
            if (!cursor.TryGotoNext(ILCursorExtensions.BottomCenterPredicate))
                throw new HookException("Couldn't find BottomCenter");

            cursor.EmitInvertEntityPoint(nameof(Entity.BottomCenter));

            if (!cursor.TryGotoNext(instr => instr.MatchLdcI4(4)))
                throw new HookException("Couldn't find ldci4(4)");

            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SuperWallJump(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();

            // Dust.Burst(this.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>("DustParticleFromSurfaceIndex"));
            cursor.GotoPrev(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();

            // Dust.Burst(this.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchLdcR4(-2));
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SwimCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SwimJumpCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SwimRiseCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SwimUnderwaterCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_UpdateCarry(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("carryOffset")))
                throw new HookException("Couldn't find carryOffset.");

            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(MoveType.After, ILCursorExtensions.UnitYPredicate))
                throw new HookException("Couldn't find get_UnitY.");

            cursor.EmitInvertVectorDelegate();
        });

        #endregion

        #region On Hooks

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
            GravityHelperModule.PlayerComponent?.SetGravity(
                GravityHelperModule.Instance.GravityBeforeReload ??
                trigger?.GravityType ??
                GravityHelperModule.Session.InitialGravity,
                playerTriggered: false);
            GravityHelperModule.Instance.GravityBeforeReload = null;
        }

        private static void Player_CassetteFlyEnd(On.Celeste.Player.orig_CassetteFlyEnd orig, Player self)
        {
            orig(self);

            SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
            if (trigger?.FireOnBubbleReturn ?? false)
                GravityHelperModule.PlayerComponent.SetGravity(trigger.GravityType);
        }

        private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Player self, int dir, int yAdd) =>
            orig(self, dir, GravityHelperModule.ShouldInvertPlayer ? -yAdd : yAdd);

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position,
            PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            GravityRefill.NumberOfCharges = 0;

            var refillIndicator = new GravityRefill.Indicator
            {
                Position = new Vector2(0f, -20f),
            };

            self.Add(new TransitionListener
                {
                    OnOutBegin = () => GravityHelperModule.Session.InitialGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal,
                },
                new GravityComponent
                {
                    CheckInvert = () => self.StateMachine.State != Player.StDreamDash && self.CurrentBooster == null,
                    UpdateVisuals = args =>
                    {
                        if (!args.Changed) return;
                        Vector2 normalLightOffset = new Vector2(0.0f, -8f);
                        Vector2 duckingLightOffset = new Vector2(0.0f, -3f);

                        self.SetNormalLightOffset(args.NewValue == GravityType.Normal ? normalLightOffset : -normalLightOffset);
                        self.SetDuckingLightOffset(args.NewValue == GravityType.Normal ? duckingLightOffset : -duckingLightOffset);

                        var starFlyBloom = self.GetStarFlyBloom();
                        if (starFlyBloom != null)
                            starFlyBloom.Y = Math.Abs(starFlyBloom.Y) * (args.NewValue == GravityType.Inverted ? 1 : -1);

                        refillIndicator.Y = Math.Abs(refillIndicator.Y) * (args.NewValue == GravityType.Inverted ? 1 : -1);
                    },
                    UpdatePosition = args =>
                    {
                        if (!args.Changed) return;
                        var collider = self.Collider ?? self.GetNormalHitbox();
                        self.Position.Y = args.NewValue == GravityType.Inverted
                            ? collider.AbsoluteTop
                            : collider.AbsoluteBottom;
                    },
                    UpdateColliders = args =>
                    {
                        if (!args.Changed) return;
                        void invertHitbox(Hitbox hitbox) => hitbox.Position.Y = -hitbox.Position.Y - hitbox.Height;
                        invertHitbox(self.GetNormalHitbox());
                        invertHitbox(self.GetNormalHurtbox());
                        invertHitbox(self.GetDuckHitbox());
                        invertHitbox(self.GetDuckHurtbox());
                        invertHitbox(self.GetStarFlyHitbox());
                        invertHitbox(self.GetStarFlyHurtbox());
                    },
                    UpdateSpeed = args =>
                    {
                        if (!args.Changed) return;
                        self.Speed.Y *= -args.MomentumMultiplier;
                        self.DashDir.Y *= -1;
                        self.SetVarJumpTimer(0f);
                    },
                },
                new DashListener
                {
                    OnDash = _ =>
                    {
                        if (GravityRefill.NumberOfCharges == 0)
                            return;
                        GravityRefill.NumberOfCharges--;
                        GravityHelperModule.PlayerComponent.SetGravity(GravityType.Toggle);
                    },
                },
                refillIndicator
            );
        }

        private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player self)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
            {
                orig(self);
                return;
            }

            var scaleY = self.Sprite.Scale.Y;
            self.Sprite.Scale.Y = -scaleY;
            orig(self);
            self.Sprite.Scale.Y = scaleY;
        }

        private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, dir);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            var rv = orig(self, new Vector2(dir.X, -dir.Y));

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            return rv;
        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self);

            var player = new DynData<Player>(self);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            var rv = orig(self);

            // if we've buffered a dream jump, don't flip the Y component back
            if (!player.Get<bool>("dreamJump"))
                self.Speed.Y *= -1;

            self.DashDir.Y *= -1;

            return rv;
        }

        private static ParticleType Player_DustParticleFromSurfaceIndex(On.Celeste.Player.orig_DustParticleFromSurfaceIndex orig, Player self, int index)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self, index);
            return index == 40 ? _invertedSparkyDustParticle.Value : _invertedDustParticle.Value;
        }

        private static bool Player_JumpThruBoostBlockedCheck(On.Celeste.Player.orig_JumpThruBoostBlockedCheck orig, Player self)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
                return orig(self);

            foreach (var component in self.Scene.Tracker.GetComponents<LedgeBlocker>())
            {
                var ledgeBlocker = (LedgeBlocker)component;
                if (!ledgeBlocker.Blocking ||
                    !self.CollideCheck(component.Entity, self.Position + Vector2.UnitY * 2f))
                    continue;

                if (ledgeBlocker.BlockChecker == null || ledgeBlocker.BlockChecker(self))
                    return true;
            }

            return false;
        }

        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
        {
            var correctState = self.StateMachine.State != Player.StStarFly &&
                               self.StateMachine.State != Player.StSwim &&
                               self.StateMachine.State != Player.StDreamDash;

            if (!correctState || self.Speed.Y >= 0)
            {
                orig(self, data);
                return;
            }

            if (GravityHelperModule.ShouldInvertPlayer && self.CollideCheckOutside<JumpThru>(self.Position + Vector2.UnitY) ||
                !GravityHelperModule.ShouldInvertPlayer && self.CollideCheckOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY))
            {
                self.Speed.Y = 0.0f;
                self.SetLastClimbMove(0);
                ReflectionCache.Player_VarJumpTimer.SetValue(self, 0);
            }

            orig(self, data);
        }

        private static void Player_ReflectBounce(On.Celeste.Player.orig_ReflectBounce orig, Player self,
            Vector2 direction) =>
            orig(self, GravityHelperModule.ShouldInvertPlayer ? new Vector2(direction.X, -direction.Y) : direction);

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            var scaleY = self.Sprite.Scale.Y;

            if (GravityHelperModule.ShouldInvertPlayer)
                self.Sprite.Scale.Y = -scaleY;

            orig(self);

            if (GravityHelperModule.ShouldInvertPlayer)
                self.Sprite.Scale.Y = scaleY;
        }

        private static void Player_StarFlyBegin(On.Celeste.Player.orig_StarFlyBegin orig, Player self)
        {
            orig(self);

            if (GravityHelperModule.ShouldInvertPlayer)
            {
                var bloom = self.GetStarFlyBloom();
                if (bloom != null)
                    bloom.Y = Math.Abs(bloom.Y);
            }
        }

        private static void Player_StartCassetteFly(On.Celeste.Player.orig_StartCassetteFly orig, Player self, Vector2 targetPosition, Vector2 control)
        {
            GravityHelperModule.PlayerComponent.SetGravity(GravityType.Normal, playerTriggered: false);
            orig(self, targetPosition, control);
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target,
            Vector2 direction)
        {
            GravityHelperModule.Transitioning = true;
            bool val = orig(self, target, direction);
            GravityHelperModule.Transitioning = false;
            return val;
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            var featherY = Input.Feather.Value.Y;
            var aimY = Input.Aim.Value.Y;
            var moveY = Input.MoveY.Value;
            var gliderMoveY = Input.GliderMoveY.Value;

            if (GravityController.VVVVVV)
            {
                var jumpPressed = Input.Jump.Pressed;
                Input.Jump.ConsumePress();

                if (jumpPressed && self.OnGround())
                {
                    GravityHelperModule.PlayerComponent.SetGravity(GravityType.Toggle);
                    self.Speed.Y = 160f * (self.SceneAs<Level>().InSpace ? 0.6f : 1f);
                }
            }

            if (GravityHelperModule.ShouldInvertPlayer)
            {
                Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, -aimY));
                Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, -featherY));
                Input.MoveY.Value = -moveY;
                Input.GliderMoveY.Value = -gliderMoveY;
            }

            orig(self);

            if (GravityHelperModule.ShouldInvertPlayer)
            {
                Input.GliderMoveY.Value = gliderMoveY;
                Input.MoveY.Value = moveY;
                Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, featherY));
                Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, aimY));
            }
        }

        private static void Player_WindMove(On.Celeste.Player.orig_WindMove orig, Player self, Vector2 move) =>
            orig(self, GravityHelperModule.ShouldInvertPlayer ? new Vector2(move.X, -move.Y) : move);

        #endregion

        private static void emitDashUpdateFixes(ILContext il)
        {
            var cursor = new ILCursor(il);

            // find DashDir.Y < 0.1 check
            cursor.GotoNext(instr => instr.MatchLdflda<Player>(nameof(Player.DashDir)),
                instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));

            // find this.CanUnDuck check
            cursor.GotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchCallvirt<Player>("get_CanUnDuck"));

            // mark it
            var target = cursor.Next;

            // jump back to before foreach
            cursor.GotoPrev(instr => instr.MatchLdarg(0),
                instr => instr.MatchCall<Entity>("get_Scene"),
                instr => instr.MatchCallvirt<Scene>("get_Tracker"));

            // emit a delegate to check UDJT
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<Player, bool>>(self =>
            {
                if (!GravityHelperModule.ShouldInvertPlayer)
                    return false;

                var ghUdjt = self.Scene.Tracker.GetEntitiesOrEmpty<UpsideDownJumpThru>().Cast<Entity>();
                var mhhUdjt = self.Scene.Tracker.GetEntitiesOrEmpty(ReflectionCache.MaxHelpingHandUpsideDownJumpThruType);
                var entities = ghUdjt.Concat(mhhUdjt);

                foreach (var entity in entities)
                {
                    if (self.CollideCheck(entity) && entity.Bottom - self.Top <= 6f &&
                        !self.CallDashCorrectCheck(Vector2.UnitY * (entity.Bottom - self.Top)))
                    {
                        self.MoveVExact((int)(self.Top - entity.Bottom));
                    }
                }

                return true;
            });

            // if gravity is inverted we should skip regular jumpthrus
            cursor.Emit(OpCodes.Brtrue_S, target);
        }

        private static void replaceGetLiftBoost(this ILCursor cursor, int count = 1) =>
            cursor.ReplaceWithDelegate<Func<Player, Vector2>>(instr => instr.MatchCallvirt<Player>("get_LiftBoost"), getLiftBoost, count);

        private static Vector2 getLiftBoost(Player player)
        {
            Vector2 liftSpeed = player.LiftSpeed;
            if (GravityHelperModule.ShouldInvertPlayer)
                liftSpeed = new Vector2(liftSpeed.X, -liftSpeed.Y);

            if (Math.Abs(liftSpeed.X) > 250f)
                liftSpeed.X = 250f * Math.Sign(liftSpeed.X);

            if (liftSpeed.Y > 0f)
                liftSpeed.Y = 0f;
            else if (liftSpeed.Y < -130f)
                liftSpeed.Y = -130f;

            return liftSpeed;
        }

        private static Lazy<ParticleType> _invertedDustParticle = new Lazy<ParticleType>(() => new ParticleType(ParticleTypes.Dust)
        {
            Acceleration = new Vector2(0.0f, -4f),
            Direction = -(float)Math.PI / 2f,
        });

        private static Lazy<ParticleType> _invertedSparkyDustParticle = new Lazy<ParticleType>(() => new ParticleType(ParticleTypes.SparkyDust)
        {
            Acceleration = new Vector2(0.0f, -4f),
            Direction = -(float)Math.PI / 2f,
        });


    }
}
