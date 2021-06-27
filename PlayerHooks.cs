using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper
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
        private delegate bool orig_get_CanUnDuck(Player self);
        // ReSharper restore InconsistentNaming

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading Player hooks...");

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
            IL.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            IL.Celeste.Player.SideBounce += Player_SideBounce;
            IL.Celeste.Player.StarFlyUpdate += Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce += Player_SuperBounce;
            IL.Celeste.Player.SuperJump += Player_SuperJump;
            IL.Celeste.Player.SuperWallJump += Player_SuperWallJump;
            IL.Celeste.Player.SwimCheck += Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck += Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck += Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck += Player_SwimUnderwaterCheck;

            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
            On.Celeste.Player.CassetteFlyEnd += Player_CassetteFlyEnd;
            On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.DustParticleFromSurfaceIndex += Player_DustParticleFromSurfaceIndex;
            On.Celeste.Player.JumpThruBoostBlockedCheck += Player_JumpThruBoostBlockedCheck;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.ReflectBounce += Player_ReflectBounce;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.SlipCheck += Player_SlipCheck;
            On.Celeste.Player.StartCassetteFly += Player_StartCassetteFly;
            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            On.Celeste.Player.Update += Player_Update;

            hook_Player_DashCoroutine = new ILHook(ReflectionCache.Player_DashCoroutine.GetStateMachineTarget(), Player_DashCoroutine);
            hook_Player_orig_Update = new ILHook(ReflectionCache.Player_OrigUpdate, Player_orig_Update);
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
            Logger.Log(nameof(GravityHelperModule), $"Unloading Player hooks...");

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
            IL.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
            IL.Celeste.Player.SideBounce -= Player_SideBounce;
            IL.Celeste.Player.StarFlyUpdate -= Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce -= Player_SuperBounce;
            IL.Celeste.Player.SuperJump -= Player_SuperJump;
            IL.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
            IL.Celeste.Player.SwimCheck -= Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck -= Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck -= Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck -= Player_SwimUnderwaterCheck;

            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.CassetteFlyEnd -= Player_CassetteFlyEnd;
            On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
            On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.DustParticleFromSurfaceIndex -= Player_DustParticleFromSurfaceIndex;
            On.Celeste.Player.JumpThruBoostBlockedCheck -= Player_JumpThruBoostBlockedCheck;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.ReflectBounce -= Player_ReflectBounce;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.SlipCheck -= Player_SlipCheck;
            On.Celeste.Player.StartCassetteFly -= Player_StartCassetteFly;
            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            On.Celeste.Player.Update -= Player_Update;

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

        private static void Player_BeforeDownTransition(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            var target = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(self =>
            {
                if (!GravityHelperModule.ShouldInvert)
                    return false;

                // copied from Player.BeforeUpTransition
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

                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, target);
            cursor.Emit(OpCodes.Ret);
        }

        private static void Player_BeforeUpTransition(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            var target = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(self =>
            {
                if (!GravityHelperModule.ShouldInvert)
                    return false;

                // copied from Player.BeforeDownTransition
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

                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, target);
            cursor.Emit(OpCodes.Ret);
        }

        private static void Player_Bounce(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
            cursor.EmitInvertIntDelegate();
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(6));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_ClimbJump(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // Dust.Burst(this.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchLdstr("event:/char/madeline/jump_climb_right"));
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();

            // Dust.Burst(this.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchLdstr("event:/char/madeline/jump_climb_left"));
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_ClimbUpdate(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // replace all calls to LiftBoost (should be 4)
            cursor.replaceGetLiftBoost(4);
            cursor.Goto(0);

            // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
            cursor.GotoNext(MoveType.After, instr => Extensions.UnitYPredicate(instr) && Extensions.SubtractionPredicate(instr.Next));
            cursor.EmitInvertVectorDelegate();

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            cursor.GotoNextAddition();
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_CreateWallSlideParticles(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // Dust.Burst(dir != 1 ? center + new Vector2(-x, 4f) : center + new Vector2(x, 4f), -1.5707964f, particleType: particleType);
            cursor.GotoNext(instr => instr.MatchCall(typeof(Dust), nameof(Dust.Burst)));
            cursor.GotoPrev(instr => instr.MatchLdcI4(1));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_ctor_OnFrameChange(ILContext il)
        {
            logCurrentMethod();

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
        }

        private static void Player_DashCoroutine(ILContext il)
        {
            logCurrentMethod();

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
        }

        private static void Player_DashUpdate(ILContext il)
        {
            logCurrentMethod();
            emitDashUpdateFixes(il);
        }

        private static void Player_get_CanUnDuck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _)))
            {
                cursor.Remove();
                cursor.EmitDelegate<Func<Player, bool>>(self =>
                    self.CollideCheck<Solid>() ||
                    !GravityHelperModule.ShouldInvert && self.CollideCheck<UpsideDownJumpThru>() ||
                    GravityHelperModule.ShouldInvert && self.CollideCheck<JumpThru>());
            }
            else
            {
                throw new Exception("Couldn't hook jumpthru checks in Player.CanUnDuck");
            }
        }

        private static void Player_IsOverWater(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdloc(0));
            cursor.EmitDelegate<Func<Rectangle, Rectangle>>(r =>
            {
                if (GravityHelperModule.ShouldInvert) r.Y -= 2;
                return r;
            });
        }

        private static void Player_Jump(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.ReplaceBottomCenterWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_LaunchedBoostCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();
        }

        private static void Player_NormalUpdate(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            cursor.replaceGetLiftBoost(3);
            cursor.Goto(0);

            // if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float) -index) && this.ClimbCheck((int) this.Facing, -index))
            cursor.GotoNextUnitY(MoveType.After);
            cursor.EmitInvertVectorDelegate();

            // if ((water = this.CollideFirst<Water>(this.Position + Vector2.UnitY * 2f)) != null)
            cursor.GotoNextUnitY(MoveType.After);
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_OnCollideH(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // (SKIP) if (this.onGround && this.DuckFreeAt(this.Position + Vector2.UnitX * (float) Math.Sign(this.Speed.X)))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.DuckFreeAt)));

            // if (!this.CollideCheck<Solid>(at) && this.CollideCheck<Solid>(at - Vector2.UnitY * (float) index2) && !this.DashCorrectCheck(add))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Vector2 at = this.Position + add;
            cursor.GotoPrev(Extensions.AdditionPredicate);
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_OnCollideV(ILContext il)
        {
            logCurrentMethod();

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
            cursor.GotoPrev(Extensions.AdditionPredicate);
            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.Position, new Vector2(0.0f, -1f).Angle(), 8, this.DustParticleFromSurfaceIndex(index));
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>("DustParticleFromSurfaceIndex"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-1));
            cursor.EmitInvertFloatDelegate();

            // ceiling pop correction
            for (int i = 0; i < 4; i++)
            {
                cursor.GotoNext(instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));
                cursor.GotoNext(Extensions.AdditionPredicate);
                cursor.EmitInvertVectorDelegate();
                cursor.Index += 2;
            }
        }

        private static void Player_orig_Update(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            /*
             * else if ((double) this.Speed.Y >= 0.0)
             * {
             *   Platform platform = (Platform) this.CollideFirst<Solid>(this.Position + Vector2.UnitY) ?? (Platform) this.CollideFirstOutside<JumpThru>(this.Position + Vector2.UnitY);
             *   if (platform != null)
             */

            // ensure we check ground collisions the right direction
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();
            // cursor.ReplaceAdditionWithDelegate();
            cursor.GotoNextAddition();

            // prevent Madeline from attempting to stand on the underside of regular jumpthrus
            // or the topside of upside down jumpthrus
            cursor.GotoNext(instr => instr.MatchLdloc(1) && instr.Next.MatchBrfalse(out _));
            var platformNotEqualNull = cursor.Next;
            cursor.GotoPrev(instr => instr.MatchLdarg(0) && instr.Next.MatchLdarg(0));
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<Player, Platform>>(self =>
                !GravityHelperModule.ShouldInvert
                    ? self.CollideFirstOutside<JumpThru>(self.Position + Vector2.UnitY)
                    : self.CollideFirstOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY));
            cursor.Emit(OpCodes.Stloc_1);
            cursor.Emit(OpCodes.Br_S, platformNotEqualNull);
            cursor.Goto(platformNotEqualNull);

            /*
             * else if (this.onGround && (this.CollideCheck<Solid, NegaBlock>(this.Position + Vector2.UnitY) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY)) && (!this.CollideCheck<Spikes>(this.Position) || SaveData.Instance.Assists.Invincible))
             */

            // ensure we check ground collisions the right direction for refilling dash on solid ground
            cursor.GotoNextAddition();
            cursor.EmitInvertVectorDelegate();

            // find some instructions
            cursor.GotoNext(instr => instr.Match(OpCodes.Ldarg_0) && instr.Next.Match(OpCodes.Ldarg_0));
            var jumpThruCheck = cursor.Next;
            cursor.GotoNextAddition(MoveType.After);
            cursor.GotoNext(instr => instr.MatchLdarg(0));
            var spikesCheck = cursor.Next;
            cursor.GotoNext(instr => instr.Match(OpCodes.Ldarg_0) && instr.Next.MatchLdfld<Player>("varJumpTimer"));
            var varJumpTimerCheck = cursor.Next;

            // replace the JumpThru check with UpsideDownJumpThru if we can and should
            cursor.Goto(jumpThruCheck);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<Player, bool>>(self =>
                !GravityHelperModule.ShouldInvert
                    ? self.CollideCheckOutside<JumpThru>(self.Position + Vector2.UnitY)
                    : self.CollideCheckOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY));
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
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();

            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) Math.Sign(this.wallSpeedRetained)))
            cursor.GotoNextAddition(MoveType.After);

            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) this.hopWaitX))
            cursor.GotoNextAddition(MoveType.After);

            // apply upside-down jumpthru correction
            if (cursor.TryGotoNext(instr => instr.MatchLdarg(0), instr => instr.MatchLdfld<Player>("onGround")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Player>>(self =>
                {
                    if (!GravityHelperModule.ShouldInvert)
                        return;

                    bool udjtBoostBlockedCheck()
                    {
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

                    if (!self.GetOnGround() && self.Speed.Y <= 0.0 &&
                        (self.StateMachine.State != Player.StClimb || self.GetLastClimbMove() == -1) &&
                        self.CollideCheck<UpsideDownJumpThru>() && !udjtBoostBlockedCheck())
                    {
                        self.MoveV(-40f * Engine.DeltaTime);
                    }
                });
            }
            else
            {
                Logger.Log(nameof(GravityHelperModule), "Couldn't apply UpsideDownJumpThru correction.");
            }

            /*
             * if (!this.onGround && this.DashAttacking && (double) this.DashDir.Y == 0.0 && (this.CollideCheck<Solid>(this.Position + Vector2.UnitY * 3f) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY * 3f)))
             *     this.MoveVExact(3);
             */

            // fix inverted ground correction for dashing (may need to ignore jumpthrus later)
            if (cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>("get_DashAttacking")))
            {
                cursor.GotoNextAddition();
                cursor.EmitInvertVectorDelegate();
                cursor.Index += 2;
                cursor.GotoNextAddition();
                cursor.EmitInvertVectorDelegate();
            }
            else
            {
                Logger.Log(nameof(GravityHelperModule), "Couldn't find get_DashAttacking");
            }

            /*
             * if (water != null && (double) this.Center.Y < (double) water.Center.Y)
             */

            // invert Center.Y check (fixes Madeline slamming into the ground when climbing down into water)
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_orig_UpdateSprite(ILContext il)
        {
            logCurrentMethod();

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

                cursor.GotoNextAddition(MoveType.After);
                cursor.GotoNextAddition(MoveType.After);

                cursor.Remove(); // TODO: not remove instructions
                cursor.EmitDelegate<Func<Player, Vector2, bool>>((self, at) =>
                    !GravityHelperModule.ShouldInvert
                        ? self.CollideCheck<JumpThru>(at)
                        : self.CollideCheck<UpsideDownJumpThru>(self.Position + new Vector2((int) self.Facing * 4, -2f)));
            }
        }

        private static void Player_orig_WallJump(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();
        }

        private static void Player_RedDashUpdate(ILContext il)
        {
            logCurrentMethod();
            emitDashUpdateFixes(il);
        }

        private static void Player_SideBounce(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // this.MoveV(Calc.Clamp(fromY - this.Bottom, -4f, 4f));
            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_StarFlyUpdate(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // this.level.Particles.Emit(FlyFeather.P_Flying, 1, this.Center, Vector2.One * 2f, (-this.Speed).Angle());
            cursor.GotoNext(instr => instr.MatchCallvirt<ParticleSystem>(nameof(ParticleSystem.Emit)));
            cursor.Index--;
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_SuperBounce(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);

            // this.MoveV(fromY - this.Bottom);
            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_SuperJump(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            // this.Speed += this.LiftBoost;
            cursor.replaceGetLiftBoost();

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            cursor.GotoNext(instr => instr.MatchCall<SurfaceIndex>(nameof(SurfaceIndex.GetPlatformByPriority)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Dust.Burst(this.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.ReplaceBottomCenterWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_SuperWallJump(ILContext il)
        {
            logCurrentMethod();

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
        }

        private static void Player_SwimCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_SwimJumpCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_SwimRiseCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        }

        private static void Player_SwimUnderwaterCheck(ILContext il)
        {
            logCurrentMethod();

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(out _));
            cursor.EmitInvertFloatDelegate();
        }

        #endregion

        #region On Hooks

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
            GravityHelperModule.Instance.SetGravity(GravityHelperModule.Instance.GravityBeforeReload ?? trigger?.GravityType ?? GravityHelperModule.Session.InitialGravity);
            GravityHelperModule.Instance.GravityRefillCharges = 0;
            GravityHelperModule.Instance.GravityBeforeReload = null;
        }

        private static void Player_CassetteFlyEnd(On.Celeste.Player.orig_CassetteFlyEnd orig, Player self)
        {
            orig(self);

            SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
            if (trigger?.FireOnBubbleReturn ?? false)
                GravityHelperModule.Instance.SetGravity(trigger.GravityType);
        }

        private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Player self, int dir, int yAdd) =>
            orig(self, dir, GravityHelperModule.ShouldInvert ? -yAdd : yAdd);

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position,
            PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            self.Add(new TransitionListener
                {
                    OnOutBegin = () => GravityHelperModule.Session.InitialGravity = GravityHelperModule.Instance.Gravity,
                },
                new GravityListener(),
                new DashListener
                {
                    OnDash = _ =>
                    {
                        if (GravityHelperModule.Instance.GravityRefillCharges == 0)
                            return;
                        GravityHelperModule.Instance.GravityRefillCharges--;
                        GravityHelperModule.Instance.SetGravity(GravityType.Toggle);
                    }
                });
        }

        private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir)
        {
            if (!GravityHelperModule.ShouldInvert)
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
            if (!GravityHelperModule.ShouldInvert)
                return orig(self);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            var rv = orig(self);

            self.Speed.Y *= -1;
            self.DashDir.Y *= -1;

            return rv;
        }

        private static ParticleType Player_DustParticleFromSurfaceIndex(On.Celeste.Player.orig_DustParticleFromSurfaceIndex orig, Player self, int index)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, index);
            return index == 40 ? invertedSparkyDustParticle.Value : invertedDustParticle.Value;
        }

        private static bool Player_JumpThruBoostBlockedCheck(On.Celeste.Player.orig_JumpThruBoostBlockedCheck orig, Player self) =>
            GravityHelperModule.ShouldInvert || orig(self);

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

            if (GravityHelperModule.ShouldInvert && self.CollideCheckOutside<JumpThru>(self.Position + Vector2.UnitY) ||
                !GravityHelperModule.ShouldInvert && self.CollideCheckOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY))
            {
                self.Speed.Y = 0.0f;
                self.SetLastClimbMove(0);
                ReflectionCache.Player_VarJumpTimer.SetValue(self, 0);
            }

            orig(self, data);
        }

        private static void Player_ReflectBounce(On.Celeste.Player.orig_ReflectBounce orig, Player self,
            Vector2 direction) =>
            orig(self, GravityHelperModule.ShouldInvert ? new Vector2(direction.X, -direction.Y) : direction);

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            var scaleY = self.Sprite.Scale.Y;

            if (GravityHelperModule.ShouldInvert)
                self.Sprite.Scale.Y = -scaleY;

            orig(self);

            if (GravityHelperModule.ShouldInvert)
                self.Sprite.Scale.Y = scaleY;
        }

        private static bool Player_SlipCheck(On.Celeste.Player.orig_SlipCheck orig, Player self, float addY)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, addY);

            Vector2 point = self.Facing != Facings.Right ? self.BottomLeft - Vector2.UnitX - Vector2.UnitY * (4f + addY) : self.BottomRight - Vector2.UnitY * (4f + addY);
            return !self.Scene.CollideCheck<Solid>(point) && !self.Scene.CollideCheck<Solid>(point - Vector2.UnitY * (addY - 4f));
        }

        private static void Player_StartCassetteFly(On.Celeste.Player.orig_StartCassetteFly orig, Player self, Vector2 targetPosition, Vector2 control)
        {
            GravityHelperModule.Instance.SetGravity(GravityType.Normal);
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
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self);
                return;
            }

            var featherY = Input.Feather.Value.Y;
            var aimY = Input.Aim.Value.Y;
            var moveY = Input.MoveY.Value;

            Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, -aimY));
            Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, -featherY));
            Input.MoveY.Value = -moveY;

            orig(self);

            Input.MoveY.Value = moveY;
            Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, -featherY));
            Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, aimY));
        }

        #endregion

        private static void emitDashUpdateFixes(ILContext il)
        {
            var cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchLdflda<Player>(nameof(Player.DashDir)),
                instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.GotoNext(instr => instr.MatchLdarg(0));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(self =>
            {
                if (!GravityHelperModule.ShouldInvert)
                    return;

                var entities = self.Scene.Tracker.GetEntitiesOrEmpty<UpsideDownJumpThru>();
                foreach (var entity in entities)
                {
                    if (self.CollideCheck(entity) && entity.Bottom - self.Top <= 6f &&
                        !self.CallDashCorrectCheck(Vector2.UnitY * (entity.Bottom - self.Top)))
                    {
                        self.MoveVExact((int)(self.Top - entity.Bottom));
                    }
                }
            });
        }

        private static void replaceGetLiftBoost(this ILCursor cursor, int count = 1) =>
            cursor.ReplaceWithDelegate<Func<Player, Vector2>>(instr => instr.MatchCallvirt<Player>("get_LiftBoost"), getLiftBoost, count);

        private static Vector2 getLiftBoost(Player player)
        {
            Vector2 liftSpeed = player.LiftSpeed;
            if (GravityHelperModule.ShouldInvert)
                liftSpeed = new Vector2(liftSpeed.X, -liftSpeed.Y);

            if (Math.Abs(liftSpeed.X) > 250f)
                liftSpeed.X = 250f * Math.Sign(liftSpeed.X);

            if (liftSpeed.Y > 0f)
                liftSpeed.Y = 0f;
            else if (liftSpeed.Y < -130f)
                liftSpeed.Y = -130f;

            return liftSpeed;
        }

        private static Lazy<ParticleType> invertedDustParticle = new Lazy<ParticleType>(() => new ParticleType(ParticleTypes.Dust)
        {
            Acceleration = new Vector2(0.0f, -4f),
            Direction = -(float)Math.PI / 2f,
        });

        private static Lazy<ParticleType> invertedSparkyDustParticle = new Lazy<ParticleType>(() => new ParticleType(ParticleTypes.SparkyDust)
        {
            Acceleration = new Vector2(0.0f, -4f),
            Direction = -(float)Math.PI / 2f,
        });

        private static void logCurrentMethod([CallerMemberName] string caller = null) =>
            Logger.Log(nameof(GravityHelperModule), $"Hooking IL {caller}");
    }
}
