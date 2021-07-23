using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities;
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
            IL.Celeste.Player.ExplodeLaunch_Vector2_bool_bool += Player_ExplodeLaunch_Vector2_bool_bool;
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
            IL.Celeste.Player.ExplodeLaunch_Vector2_bool_bool -= Player_ExplodeLaunch_Vector2_bool_bool;
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
                if (!GravityHelperModule.ShouldInvert)
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
                if (!GravityHelperModule.ShouldInvert)
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

            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
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
                instr => Extensions.UnitYPredicate(instr) && Extensions.SubtractionPredicate(instr.Next));
            cursor.EmitInvertVectorDelegate();

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            cursor.GotoNextAddition();
            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_CreateWallSlideParticles(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // Dust.Burst(dir != 1 ? center + new Vector2(-x, 4f) : center + new Vector2(x, 4f), -1.5707964f, particleType: particleType);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
                throw new HookException("Couldn't match first instance of 4f");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
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
                !GravityHelperModule.ShouldInvert && self.CollideCheck<UpsideDownJumpThru>() ||
                GravityHelperModule.ShouldInvert && self.CollideCheck<JumpThru>());
        });

        private static void Player_ExplodeLaunch_Vector2_bool_bool(ILContext il) => HookUtils.SafeHook(() => {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchLdfld<PlayerInventory>(nameof(PlayerInventory.NoRefills))) ||
                !cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Player>(nameof(Player.Speed))))
                throw new HookException("Couldn't invert SlashFx.Burst direction.");

            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(instr => instr.MatchRet()))
                throw new HookException("Couldn't find return.");

            cursor.EmitInvertVectorDelegate();
        });

        private static void Player_IsOverWater(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdloc(0));
            cursor.EmitDelegate<Func<Rectangle, Rectangle>>(r =>
            {
                if (GravityHelperModule.ShouldInvert) r.Y -= 2;
                return r;
            });
        });

        private static void Player_Jump(ILContext il) => HookUtils.SafeHook(() =>
        {
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

            // (SKIP) if (this.onGround && this.DuckFreeAt(this.Position + Vector2.UnitX * (float) Math.Sign(this.Speed.X)))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.DuckFreeAt)));

            // if (!this.CollideCheck<Solid>(at) && this.CollideCheck<Solid>(at - Vector2.UnitY * (float) index2) && !this.DashCorrectCheck(add))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            cursor.EmitInvertVectorDelegate();

            // Vector2 at = this.Position + add;
            cursor.GotoPrev(Extensions.AdditionPredicate);
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
            if (!cursor.TryGotoNext(Extensions.AdditionPredicate) ||
                !cursor.TryGotoNext(instr => instr.MatchLdloc(1) && instr.Next.MatchBrfalse(out _)))
                throw new HookException("Couldn't find platform != null check.");

            var platformNotEqualNull = cursor.Next;
            if (!cursor.TryGotoPrev(instr => instr.MatchLdarg(0) && instr.Next.MatchLdarg(0)))
                throw new HookException("Couldn't apply patch for jumpthrus.");

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
            if (!cursor.TryGotoNext(Extensions.AdditionPredicate))
                throw new HookException("Couldn't apply patch for dash refill.");

            cursor.EmitInvertVectorDelegate();

            // find some instructions
            if (!cursor.TryGotoNext(instr => instr.Match(OpCodes.Ldarg_0) && instr.Next.Match(OpCodes.Ldarg_0)))
                throw new HookException("Couldn't find jumpthru check.");

            var jumpThruCheck = cursor.Next;

            if (!cursor.TryGotoNext(MoveType.After, Extensions.AdditionPredicate) ||
                !cursor.TryGotoNext(instr => instr.MatchLdarg(0)))
                throw new HookException("Couldn't find spikes check.");

            var spikesCheck = cursor.Next;

            if (!cursor.TryGotoNext(instr =>
                instr.Match(OpCodes.Ldarg_0) && instr.Next.MatchLdfld<Player>("varJumpTimer")))
                throw new HookException("Couldn't find varJumpTimer check");

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
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
                throw new HookException("Couldn't apply moving block check.");

            cursor.EmitInvertFloatDelegate();

            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) Math.Sign(this.wallSpeedRetained)))
            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) this.hopWaitX))
            if (!cursor.TryGotoNext(MoveType.After, Extensions.AdditionPredicate) ||
                !cursor.TryGotoNext(MoveType.After, Extensions.AdditionPredicate))
                throw new HookException("Couldn't skip two additions.");

            // apply upside-down jumpthru correction
            if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0), instr => instr.MatchLdfld<Player>("onGround")) ||
                !cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _),
                    instr => instr.MatchBrfalse(out _)))
                throw new HookException("Couldn't apply UpsideDownJumpThru dash correction.");

            cursor.Remove();
            cursor.EmitDelegate<Func<Player, bool>>(self => GravityHelperModule.ShouldInvert
                ? self.CollideCheck<UpsideDownJumpThru>()
                : self.CollideCheck<JumpThru>());

            /*
             * if (!this.onGround && this.DashAttacking && (double) this.DashDir.Y == 0.0 && (this.CollideCheck<Solid>(this.Position + Vector2.UnitY * 3f) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY * 3f)))
             *     this.MoveVExact(3);
             */

            // fix inverted ground correction for dashing
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>("get_DashAttacking")))
                throw new HookException("Couldn't find get_DashAttacking");

            if (!cursor.TryGotoNext(Extensions.AdditionPredicate))
                throw new HookException("Couldn't find addition.");

            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheckOutside), out _)))
                throw new HookException("Couldn't find generic CollideCheckOutside.");

            cursor.Remove();
            cursor.EmitDelegate<Func<Entity, Vector2, bool>>((self, at) =>
                !GravityHelperModule.ShouldInvert
                    ? self.CollideCheckOutside<JumpThru>(at)
                    : self.CollideCheckOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY * 3f));

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
                    !GravityHelperModule.ShouldInvert
                        ? self.CollideCheck<JumpThru>(at)
                        : self.CollideCheck<UpsideDownJumpThru>(at));
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
            if (!cursor.TryGotoNext(MoveType.After, Extensions.SubtractionPredicate))
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
            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
            cursor.EmitInvertFloatDelegate();
        });

        private static void Player_SlipCheck(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(instr => instr.MatchCall<Entity>("get_TopRight")))
                throw new HookException("Couldn't replace TopRight with BottomRight while inverted");
            cursor.Remove();
            cursor.EmitDelegate<Func<Entity, Vector2>>(e => GravityHelperModule.ShouldInvert ? e.BottomRight : e.TopRight);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
                throw new HookException("Couldn't replace 4 with 5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvert ? f + 1 : f);

            if (!cursor.TryGotoNext(Extensions.AdditionPredicate))
                throw new HookException("Couldn't replace vector addition with subtraction");
            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(instr => instr.MatchCall<Entity>("get_TopLeft")))
                throw new HookException("Couldn't replace TopLeft with BottomLeft while inverted");
            cursor.Remove();
            cursor.EmitDelegate<Func<Entity, Vector2>>(e => GravityHelperModule.ShouldInvert ? e.BottomLeft : e.TopLeft);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4)))
                throw new HookException("Couldn't replace 4 with 5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvert ? f + 1 : f);

            if (!cursor.TryGotoNext(Extensions.AdditionPredicate))
                throw new HookException("Couldn't replace vector addition with subtraction");
            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-4)))
                throw new HookException("Couldn't replace -4 with -5 while inverted");
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvert ? f - 1 : f);

            if (!cursor.TryGotoNext(Extensions.AdditionPredicate))
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
            cursor.ReplaceBottomWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdnull() && instr.Next.MatchLdnull());
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
            cursor.ReplaceBottomCenterWithDelegate();
            cursor.GotoNext(instr => instr.MatchLdcI4(4));
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

        private static bool Player_JumpThruBoostBlockedCheck(On.Celeste.Player.orig_JumpThruBoostBlockedCheck orig, Player self)
        {
            if (!GravityHelperModule.ShouldInvert)
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

        private static void Player_StarFlyBegin(On.Celeste.Player.orig_StarFlyBegin orig, Player self)
        {
            orig(self);

            if (GravityHelperModule.ShouldInvert)
            {
                var bloom = self.GetStarFlyBloom();
                if (bloom != null)
                    bloom.Y = Math.Abs(bloom.Y);
            }
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
            var featherY = Input.Feather.Value.Y;
            var aimY = Input.Aim.Value.Y;
            var moveY = Input.MoveY.Value;

            if (GravityHelperModule.ShouldInvert)
            {
                Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, -aimY));
                Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, -featherY));
                Input.MoveY.Value = -moveY;
            }

            orig(self);

            if (GravityHelperModule.ShouldInvert)
            {
                Input.MoveY.Value = moveY;
                Input.Feather.SetValue(new Vector2(Input.Feather.Value.X, featherY));
                Input.Aim.SetValue(new Vector2(Input.Aim.Value.X, aimY));
            }

            // flip crown in MaddyCrown if loaded
            if (ReflectionCache.GetMaddyCrownModuleSprite() is { } mcs)
            {
                mcs.Position.Y = Math.Abs(mcs.Position.Y) * (GravityHelperModule.ShouldInvert ? 1 : -1);
                mcs.Scale.Y = GravityHelperModule.ShouldInvert ? -1 : 1;
            }
        }

        private static void Player_WindMove(On.Celeste.Player.orig_WindMove orig, Player self, Vector2 move) =>
            orig(self, GravityHelperModule.ShouldInvert ? new Vector2(move.X, -move.Y) : move);

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
                if (!GravityHelperModule.ShouldInvert)
                    return false;

                var entities = self.Scene.Tracker.GetEntitiesOrEmpty<UpsideDownJumpThru>();
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


    }
}
