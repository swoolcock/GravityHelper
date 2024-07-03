// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

//#define REPLACE_LIFTBOOST

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class PlayerHooks
{
    // ReSharper disable InconsistentNaming
    private static IDetour hook_Player_DashCoroutine;
    private static IDetour hook_Player_IntroJumpCoroutine;
    private static IDetour hook_Player_PickupCoroutine;
    private static IDetour hook_Player_orig_Update;
    private static IDetour hook_Player_orig_UpdateSprite;
    private static IDetour hook_Player_orig_WallJump;
    private static IDetour hook_Player_ctor_OnFrameChange;
    private static IDetour hook_Player_get_CanUnDuck;
    private static IDetour hook_Player_get_CameraTarget;
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
        IL.Celeste.Player.GetChasePosition += Player_GetChasePosition;
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
        IL.Celeste.Player.SummitLaunchUpdate += Player_SummitLaunchUpdate;
        IL.Celeste.Player.SuperBounce += Player_SuperBounce;
        IL.Celeste.Player.SuperJump += Player_SuperJump;
        IL.Celeste.Player.SuperWallJump += Player_SuperWallJump;
        IL.Celeste.Player.SwimCheck += Player_SwimCheck;
        IL.Celeste.Player.SwimJumpCheck += Player_SwimJumpCheck;
        IL.Celeste.Player.SwimRiseCheck += Player_SwimRiseCheck;
        IL.Celeste.Player.SwimUnderwaterCheck += Player_SwimUnderwaterCheck;
        IL.Celeste.Player.UpdateCarry += Player_UpdateCarry;
        IL.Celeste.Player.UpdateChaserStates += Player_UpdateChaserStates;

        On.Celeste.Player.ctor += Player_ctor;
        On.Celeste.Player.Added += Player_Added;
        On.Celeste.Player.BoostUpdate += Player_BoostUpdate;
        On.Celeste.Player.BoostEnd += Player_BoostEnd;
        On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
        On.Celeste.Player.ClimbJump += Player_ClimbJump;
        On.Celeste.Player.CassetteFlyBegin += Player_CassetteFlyBegin;
        On.Celeste.Player.CassetteFlyEnd += Player_CassetteFlyEnd;
        On.Celeste.Player.CreateTrail += Player_CreateTrail;
        On.Celeste.Player.DoFlingBird += Player_DoFlingBird;
        On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
        On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
        On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        On.Celeste.Player.DustParticleFromSurfaceIndex += Player_DustParticleFromSurfaceIndex;
        On.Celeste.Player.ExplodeLaunch_Vector2_bool_bool += Player_ExplodeLaunch_Vector2_bool_bool;
        On.Celeste.Player.Jump += Player_Jump;
        On.Celeste.Player.JumpThruBoostBlockedCheck += Player_JumpThruBoostBlockedCheck;
        On.Celeste.Player.ReflectBounce += Player_ReflectBounce;
        On.Celeste.Player.Render += Player_Render;
        On.Celeste.Player.StarFlyBegin += Player_StarFlyBegin;
        On.Celeste.Player.SuperJump += Player_SuperJump;
        On.Celeste.Player.TransitionTo += Player_TransitionTo;
        On.Celeste.Player.Update += Player_Update;
        On.Celeste.Player.WindMove += Player_WindMove;

        using (new DetourContext { Before = { "MaxHelpingHand", "SpringCollab2020" }}) {
            hook_Player_orig_Update = new ILHook(ReflectionCache.Player_OrigUpdate, Player_orig_Update);

            hook_Player_DashCoroutine = new ILHook(ReflectionCache.Player_DashCoroutine.GetStateMachineTarget(), Player_DashCoroutine);
            hook_Player_IntroJumpCoroutine = new ILHook(ReflectionCache.Player_IntroJumpCoroutine.GetStateMachineTarget(), Player_IntroJumpCoroutine);
            hook_Player_PickupCoroutine = new ILHook(ReflectionCache.Player_PickupCoroutine.GetStateMachineTarget(), Player_PickupCoroutine);
            hook_Player_orig_UpdateSprite = new ILHook(ReflectionCache.Player_OrigUpdateSprite, Player_orig_UpdateSprite);
            hook_Player_orig_WallJump = new ILHook(ReflectionCache.Player_OrigWallJump, Player_orig_WallJump);
            hook_Player_get_CanUnDuck = new ILHook(ReflectionCache.Player_CanUnDuck, Player_get_CanUnDuck);
            hook_Player_get_CameraTarget = new ILHook(ReflectionCache.Player_CameraTarget, Player_get_CameraTarget);

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
        IL.Celeste.Player.GetChasePosition -= Player_GetChasePosition;
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
        IL.Celeste.Player.SummitLaunchUpdate -= Player_SummitLaunchUpdate;
        IL.Celeste.Player.SuperBounce -= Player_SuperBounce;
        IL.Celeste.Player.SuperJump -= Player_SuperJump;
        IL.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
        IL.Celeste.Player.SwimCheck -= Player_SwimCheck;
        IL.Celeste.Player.SwimJumpCheck -= Player_SwimJumpCheck;
        IL.Celeste.Player.SwimRiseCheck -= Player_SwimRiseCheck;
        IL.Celeste.Player.SwimUnderwaterCheck -= Player_SwimUnderwaterCheck;
        IL.Celeste.Player.UpdateCarry -= Player_UpdateCarry;
        IL.Celeste.Player.UpdateChaserStates -= Player_UpdateChaserStates;

        On.Celeste.Player.ctor -= Player_ctor;
        On.Celeste.Player.Added -= Player_Added;
        On.Celeste.Player.BoostUpdate -= Player_BoostUpdate;
        On.Celeste.Player.BoostEnd -= Player_BoostEnd;
        On.Celeste.Player.CassetteFlyBegin -= Player_CassetteFlyBegin;
        On.Celeste.Player.CassetteFlyEnd -= Player_CassetteFlyEnd;
        On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
        On.Celeste.Player.ClimbJump -= Player_ClimbJump;
        On.Celeste.Player.CreateTrail -= Player_CreateTrail;
        On.Celeste.Player.DoFlingBird -= Player_DoFlingBird;
        On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
        On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
        On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        On.Celeste.Player.DustParticleFromSurfaceIndex -= Player_DustParticleFromSurfaceIndex;
        On.Celeste.Player.ExplodeLaunch_Vector2_bool_bool -= Player_ExplodeLaunch_Vector2_bool_bool;
        On.Celeste.Player.Jump -= Player_Jump;
        On.Celeste.Player.JumpThruBoostBlockedCheck -= Player_JumpThruBoostBlockedCheck;
        On.Celeste.Player.ReflectBounce -= Player_ReflectBounce;
        On.Celeste.Player.Render -= Player_Render;
        On.Celeste.Player.StarFlyBegin -= Player_StarFlyBegin;
        On.Celeste.Player.SuperJump -= Player_SuperJump;
        On.Celeste.Player.TransitionTo -= Player_TransitionTo;
        On.Celeste.Player.Update -= Player_Update;
        On.Celeste.Player.WindMove -= Player_WindMove;

        hook_Player_DashCoroutine?.Dispose();
        hook_Player_DashCoroutine = null;

        hook_Player_IntroJumpCoroutine?.Dispose();
        hook_Player_IntroJumpCoroutine = null;

        hook_Player_PickupCoroutine?.Dispose();
        hook_Player_PickupCoroutine = null;

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

        hook_Player_get_CameraTarget?.Dispose();
        hook_Player_get_CameraTarget = null;
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
            if (self.StateMachine.State != Player.StRedDash)
                self.Speed.X = 0.0f;

            if (self.StateMachine.State != Player.StRedDash && self.StateMachine.State != Player.StReflectionFall &&
                self.StateMachine.State != Player.StStarFly)
            {
                self.varJumpSpeed = self.Speed.Y = -105f;
                self.StateMachine.State = self.StateMachine.State != Player.StSummitLaunch
                    ? Player.StNormal
                    : Player.StIntroJump;
                self.AutoJump = true;
                self.AutoJumpTimer = 0.0f;
                self.varJumpTimer = 0.2f;
            }

            self.dashCooldownTimer = 0.2f;

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
                self.varJumpTimer = 0.0f;
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

#if REPLACE_LIFTBOOST
        // replace all calls to LiftBoost (should be 4)
        cursor.replaceGetLiftBoost(4);
        cursor.Goto(0);
#endif

        // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
        cursor.GotoNext(MoveType.After,
            instr => ILCursorExtensions.UnitYPredicate(instr) && ILCursorExtensions.SubtractionPredicate(instr.Next));
        cursor.EmitInvertVectorDelegate();

        // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
        cursor.GotoNextAddition();
        cursor.EmitInvertVectorDelegate();

        // borrowed from MaddieHelpingHand
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

        static bool collideJumpthrus(Player self) =>
            !GravityHelperModule.ShouldInvertPlayer && self.CollideCheckUpsideDownJumpThru() ||
            GravityHelperModule.ShouldInvertPlayer && self.CollideCheckNotUpsideDownJumpThru();

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _)))
            throw new HookException("Couldn't find CollideCheck<Solid>");

        // check whether the unducked hitbox collides with any jumpthrus for the gravity state
        // if standing would collide but ducking does not, we cannot unduck
        // if ducking collides, we ignore it and we can unduck
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<bool, Player, bool>>((didCollideSolid, self) =>
        {
            if (didCollideSolid) return true;

            // check if ducking collides
            var currentCollider = self.Collider;
            self.Collider = self.duckHitbox;
            var didCollideJumpthrus = collideJumpthrus(self);
            self.Collider = currentCollider;

            return !didCollideJumpthrus && collideJumpthrus(self);
        });
    });

    private static void Player_get_CameraTarget(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // invert feather offset
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.2f)) ||
            !cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.2f)))
            throw new HookException("Couldn't find second 0.2f");
        cursor.EmitInvertFloatDelegate();

        // invert red dash offset
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(48)) ||
            !cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(48)))
            throw new HookException("Couldn't find second 48");
        cursor.EmitInvertIntDelegate();

        // invert summit launch offset
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(64f)))
            throw new HookException("Couldn't find 64");
        cursor.EmitInvertFloatDelegate();
    });

    private static void Player_ExplodeLaunch_Vector2_bool_bool(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        // invert the angle passed to SlashFx.Burst
        cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<SlashFx>(nameof(SlashFx.Burst)));
        cursor.EmitInvertFloatDelegate();
    });

    private static void Player_GetChasePosition(ILContext il) => HookUtils.SafeHook(() =>
    {
        //Weird ordering, but this does make sense
        var cursor = new ILCursor(il);
        VariableDefinition counter = new VariableDefinition(il.Import(typeof(int)));
        il.Body.Variables.Add(counter);
        ILLabel label = null;
        if (!cursor.TryGotoNext(instr => instr.MatchStloc(out int _), instr => instr.MatchBr(out label))) //Because we know we have a set value for label, it's safe to use.
            throw new HookException("Start of loop in Player::GetChasePosition not found");
        cursor.Index++; //to before Br
        cursor.Emit(OpCodes.Ldc_I4_M1); // load -1 to stack
        cursor.Emit(OpCodes.Stloc, counter); // set counter to -1
        if (!cursor.TryGotoNext(instr => instr.MatchLdloca(1), instr => instr.MatchCall("System.Collections.Generic.List`1/Enumerator<Celeste.Player/ChaserState>", "MoveNext")))
            throw new HookException("MoveNext in loop not found.");
    });

    private static void Player_IntroJumpCoroutine(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        var playerVar = il.Body.Variables.First(v => v.VariableType.FullName == typeof(Player).FullName);

        // player.Y = (float) (player.level.Bounds.Bottom + 16);
        if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Entity>("set_Y")))
            throw new HookException("Couldn't find Entity.set_Y");
        cursor.Emit(OpCodes.Ldloc, playerVar);
        cursor.EmitDelegate<Func<float, Player, float>>((y, self) =>
            GravityHelperModule.ShouldInvertPlayer ? self.SceneAs<Level>().Bounds.Top - 16 : y);

        // start.Y = (float) (player.level.Bounds.Bottom - 24);
        if (!cursor.TryGotoNext(instr => instr.MatchStfld<Vector2>(nameof(Vector2.Y))))
            throw new HookException("Couldn't find stfld Vector2.Y");
        cursor.Emit(OpCodes.Ldloc, playerVar);
        cursor.EmitDelegate<Func<float, Player, float>>((y, self) =>
        {
            if (!GravityHelperModule.ShouldInvertPlayer) return y;
            var level = self.level;
            return level.Bounds.Top + 24;
        });

        // player.Y += -120f * Engine.DeltaTime;
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-120f)))
            throw new HookException("Couldn't find -120");
        cursor.EmitInvertFloatDelegate();

        // while ((double) player.Y > (double) start.Y - 8.0)
        if (!cursor.TryGotoNext(instr => instr.MatchLdcR4(8), instr => instr.MatchSub()))
            throw new HookException("Couldn't find 8");
        cursor.Index++;
        cursor.EmitInvertFloatDelegate();

        ILLabel bgtLabel = null;
        if (!cursor.TryGotoNext(instr => instr.MatchBgt(out bgtLabel)))
            throw new HookException("Couldn't find bgt");
        cursor.EmitLoadShouldInvert();
        cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
        cursor.Emit(OpCodes.Blt_S, bgtLabel);
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

        // find the start of particles
        cursor.TryGotoNext(instr => instr.MatchLdloc(1),
            instr => instr.MatchLdfld<Player>("level"),
            instr => instr.MatchLdfld<Level>(nameof(Level.Particles)));

        // find the end of particles
        var cursor2 = cursor.Clone();
        cursor2.TryGotoNext(instr => instr.MatchLdarg(0),
            instr => instr.MatchLdcR4(0.35f));

        // emit new particles if required (easier than editing)
        cursor.EmitLoadShouldInvert();
        cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
        cursor.Emit(OpCodes.Ldloc, playerVar);
        cursor.EmitDelegate<Action<Player>>(self =>
        {
            var level = self.level;
            var particles = level.Particles;
            var particlesBG = level.ParticlesBG;
            particles.Emit(Player.P_SummitLandA, 12, self.TopCenter, Vector2.UnitX * 3f, (float)Math.PI / 2f);
            particles.Emit(_invertedSummitLandBParticle.Value, 8, self.TopCenter - Vector2.UnitX * 2f, Vector2.UnitX * 2f, -(float)Math.PI * 13f / 12f);
            particles.Emit(_invertedSummitLandBParticle.Value, 8, self.TopCenter + Vector2.UnitX * 2f, Vector2.UnitX * 2f, (float)Math.PI / 12f);
            particlesBG.Emit(_invertedSummitLandCParticle.Value, 30, self.TopCenter, Vector2.UnitX * 5f);
        });
        cursor.Emit(OpCodes.Br_S, cursor2.Next);
    });

    private static void Player_PickupCoroutine(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // invert the initial offset
        if (!cursor.TryGotoNext(MoveType.After, ILCursorExtensions.SubtractionPredicate))
            throw new HookException("Couldn't find vector subtraction");

        cursor.EmitInvertVectorDelegate();
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

#if REPLACE_LIFTBOOST
        // this.Speed += this.LiftBoost;
        cursor.replaceGetLiftBoost();
#endif

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
#if REPLACE_LIFTBOOST
        // this.Speed += this.LiftBoost;
        cursor.replaceGetLiftBoost();
#endif
    });

    private static void Player_NormalUpdate(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

#if REPLACE_LIFTBOOST
        cursor.replaceGetLiftBoost(3);
        cursor.Goto(0);
#endif

        /* FIX 2 pixel wall grab leniency
        if (!SaveData.Instance.Assists.NoGrabbing && (double) (float) Input.MoveY < 1.0 && (double) this.level.Wind.Y <= 0.0)
        {
          for (int index = 1; index <= 2; ++index)
          {
            if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float) -index) && this.ClimbCheck((int) this.Facing, -index))
            {
              this.MoveVExact(-index);
              this.Ducking = false;
              return 1;
            }
          }
        }
        */
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdflda<Level>(nameof(Level.Wind)),
            instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
            throw new HookException("Couldn't find Level.Wind.Y");

        // flip the Wind.Y check, since we're grabbing the other way now
        cursor.EmitInvertFloatDelegate();

        // find the Position.Y
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)),
            instr => instr.MatchCall<Vector2>("get_UnitY"),
            instr => instr.MatchLdloc(out _)))
            throw new HookException("Couldn't find Vector2.UnitY * -index");

        // invert
        cursor.EmitInvertIntDelegate();

        // find CollideCheck<Solid>
        GenericInstanceMethod collideCheckSolid = null;
        if (!cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out collideCheckSolid)))
            throw new HookException("Couldn't find CollideCheck<Solid>.");

        // add a new vector2 local
        var firstParam = collideCheckSolid.Parameters.First();
        var vecDef = new VariableDefinition(firstParam.ParameterType);
        il.Body.Variables.Add(vecDef);

        // take a local copy of the vector2 argument to CollideCheck<Solid>
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Stloc, vecDef);

        // skip to the brtrue
        if (!cursor.TryGotoNext(instr => instr.MatchBrtrue(out _)))
            throw new HookException("Couldn't find brtrue");

        // add an additional check for jumpthrus
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, vecDef);
        cursor.EmitDelegate<Func<bool, Entity, Vector2, bool>>((ccs, self, at) =>
        {
            // if CollideCheck<Solid> already returned true, nothing to do
            if (ccs) return true;
            // collide with an upside down jumpthru (relative to the player's gravity)
            return GravityHelperModule.ShouldInvertPlayer
                ? self.CollideCheckNotUpsideDownJumpThru(at)
                : self.CollideCheckUpsideDownJumpThru(at);
        });

        if (!cursor.TryGotoNext(instr => instr.MatchStfld<Player>("wallSlideDir")))
            throw new HookException("Couldn't find wallSlideDir");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<int, Player, int>>((value, self) =>
        {
            if (self.Scene is Level level &&
                level.GetActiveController<VvvvvvGravityController>(true) is { } controller &&
                controller.IsVvvvvv &&
                controller.DisableGrab)
                return 0;

            return value;
        });

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
        static bool collideJumpthrus(Player self, Vector2 at) =>
            !GravityHelperModule.ShouldInvertPlayer && self.CollideCheckUpsideDownJumpThru(at) ||
            GravityHelperModule.ShouldInvertPlayer && self.CollideCheckNotUpsideDownJumpThru(at);

        var cursor = new ILCursor(il);

        // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
        cursor.GotoNext(instr => instr.MatchCallvirt<Player>("get_DashAttacking"));
        cursor.GotoNext(MoveType.After, instr => instr.MatchCall("System.Math", nameof(Math.Sign)));
        cursor.EmitInvertIntDelegate();

        // this.ReflectBounce(new Vector2(0.0f, (float) -Math.Sign(this.Speed.Y)));
        cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ReflectBounce)));
        cursor.EmitInvertVectorDelegate();

        // invert next two `MoveVExact(1)`
        for (int i = 0; i < 2; i++)
        {
            cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveVExact)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdarg(0), instr => instr.MatchLdcI4(1));
            cursor.EmitInvertIntDelegate();
            cursor.GotoNext(instr => instr.MatchPop());
        }

        // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + new Vector2(0.0f, 1f), this.temp));
        cursor.GotoNext(instr => instr.MatchCall<SurfaceIndex>(nameof(SurfaceIndex.GetPlatformByPriority)));
        cursor.GotoPrev(ILCursorExtensions.AdditionPredicate);
        cursor.EmitInvertVectorDelegate();

        // Dust.Burst(this.Position, new Vector2(0.0f, -1f).Angle(), 8, this.DustParticleFromSurfaceIndex(index));
        cursor.GotoNext(instr => instr.MatchCallvirt<Player>("DustParticleFromSurfaceIndex"));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-1));
        cursor.EmitInvertFloatDelegate();

        // ceiling pop correction
        for (int i = -1; i <= 1; i += 2)
        {
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));
            if (!cursor.Next.MatchLdloc(out int indexLoc))
                throw new HookException("Couldn't match ldloc");

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-1));
            cursor.EmitInvertFloatDelegate();

            var sign = i;
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallGeneric<Entity>(nameof(Entity.CollideCheck), out _));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, indexLoc);
            cursor.EmitDelegate<Func<bool, Player, int, bool>>((collideSolid, self, index) =>
            {
                if (collideSolid) return true;
                var direction = GravityHelperModule.ShouldInvertPlayer ? 1f : -1f;
                var at = self.Position + new Vector2(index * sign, direction);
                return collideJumpthrus(self, at);
            });

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-1));
            cursor.EmitInvertFloatDelegate();
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
                ? self.CollideFirstOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY)
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
                ? self.CollideCheckOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY)
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
                ? self.CollideCheckOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY * 3f)
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
            cursor.Remove(); // TODO: NOT REMOVE
            cursor.EmitDelegate<Func<Player, Vector2, bool>>((self, at) =>
                !GravityHelperModule.ShouldInvertPlayer
                    ? self.CollideCheckNotUpsideDownJumpThru(at)
                    : self.CollideCheckUpsideDownJumpThru(at));
        }
    });

    private static void Player_orig_WallJump(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
#if REPLACE_LIFTBOOST
        // this.Speed += this.LiftBoost;
        cursor.replaceGetLiftBoost();
#endif
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

    private static void Player_SummitLaunchUpdate(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdcR4(40f)))
            throw new HookException("Couldn't find 40f");
        cursor.EmitLoadShouldInvert();
        cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
        cursor.Emit(OpCodes.Ldc_R4, -(40f - 12f));
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
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

#if REPLACE_LIFTBOOST
        // this.Speed += this.LiftBoost;
        cursor.replaceGetLiftBoost();
#endif

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

#if REPLACE_LIFTBOOST
        // this.Speed += this.LiftBoost;
        cursor.replaceGetLiftBoost();
#endif

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

    private static void Player_UpdateChaserStates(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(instr => instr.MatchCallvirt("System.Collections.Generic.List`1<Celeste.Player/ChaserState>", "RemoveAt")))
            throw new HookException("Couldn't find RemoveAt");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<int, Player, int>>((i, p) =>
        {
            BadelineOldsiteHooks.RemoveGravityTypeForState(p.ChaserStates[i].TimeStamp);
            return i;
        });

        //This ideally would be an infix Func<int,Player,int> hook here but it doesn't matter since who is gonna change the RemoveAt of 0 in this, realistically.

        cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt("System.Collections.Generic.List`1<Celeste.Player/ChaserState>", "Add"));
        cursor.EmitDelegate<Func<Player.ChaserState, Player.ChaserState>>(p =>
        {
            BadelineOldsiteHooks.SetGravityTypeForState(p.TimeStamp, GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);
            return p;
        });
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
            GravityHelperModule.Session.InitialGravity);
        GravityHelperModule.Instance.GravityBeforeReload = null;

        if (self.CollideFirstOrDefault<VvvvvvTrigger>() is { } vvvvvvTrigger)
            GravityHelperModule.Session.VvvvvvTrigger = vvvvvvTrigger.Enable;
        else
            GravityHelperModule.Session.VvvvvvTrigger = false;

        scene.Add(new GravityRefillIndicator());
        scene.Add(new GravityShieldIndicator());
    }

    private static void Player_CassetteFlyBegin(On.Celeste.Player.orig_CassetteFlyBegin orig, Player self)
    {
        GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Normal);
        GravityHelperModule.PlayerComponent?.Lock();
        orig(self);
    }

    private static void Player_CassetteFlyEnd(On.Celeste.Player.orig_CassetteFlyEnd orig, Player self)
    {
        orig(self);

        GravityHelperModule.PlayerComponent?.Unlock();

        SpawnGravityTrigger trigger = self.CollideFirstOrDefault<SpawnGravityTrigger>();
        if (trigger?.FireOnBubbleReturn ?? false)
            GravityHelperModule.PlayerComponent?.SetGravity(trigger.GravityType);
    }

    private static int Player_BoostUpdate(On.Celeste.Player.orig_BoostUpdate orig, Player self)
    {
        GravityHelperModule.OverrideSemaphore++;
        var rv = orig(self);
        GravityHelperModule.OverrideSemaphore--;
        return rv;
    }

    private static void Player_BoostEnd(On.Celeste.Player.orig_BoostEnd orig, Player self)
    {
        GravityHelperModule.OverrideSemaphore++;
        orig(self);
        GravityHelperModule.OverrideSemaphore--;
    }

    private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Player self, int dir, int yAdd) =>
        orig(self, dir, GravityHelperModule.ShouldInvertPlayer ? -yAdd : yAdd);

    private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self)
    {
        var oldFacing = self.Facing;
        var handled = handleInversionBlocks(self);

        if (handled && oldFacing == self.Facing)
        {
            // if we were warped and kept the same facing, we shouldn't lose stamina
            self.WallJump((int)self.Facing);
        }
        else
        {
            orig(self);
        }
    }

    private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position,
        PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);

        BadelineOldsiteHooks.ChaserStateGravity.Clear();
        GravityRefill.NumberOfCharges = 0;

        self.Add(new TransitionListener
            {
                OnOutBegin = () => GravityHelperModule.Session.InitialGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal,
            },
            new PlayerGravityComponent(),
            new DashListener
            {
                OnDash = _ =>
                {
                    if (GravityRefill.NumberOfCharges > 0 ||
                        (self.SceneAs<Level>()?.GetActiveController<BehaviorGravityController>()?.DashToToggle ?? false))
                    {
                        GravityRefill.NumberOfCharges = Math.Max(GravityRefill.NumberOfCharges - 1, 0);
                        GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Toggle);
                    }
                },
            }
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

    private static bool Player_DoFlingBird(On.Celeste.Player.orig_DoFlingBird orig, Player self, FlingBird bird)
    {
        if (GravityHelperModule.ShouldInvertPlayer)
            self.SetGravity(GravityType.Normal, 0f);
        return orig(self, bird);
    }

    private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
    {
        orig(self);

        var dreamBlock = self.dreamBlock;
        if (GravityHelperModule.PlayerComponent != null && dreamBlock is GravityDreamBlock gravityDreamBlock)
            gravityDreamBlock.PlayerEntered();
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

        self.Speed.Y *= -1;
        self.DashDir.Y *= -1;

        var rv = orig(self);

        // if we've buffered a dream jump, don't flip the Y component back
        if (!self.dreamJump)
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

    private static Vector2 Player_ExplodeLaunch_Vector2_bool_bool(On.Celeste.Player.orig_ExplodeLaunch_Vector2_bool_bool orig, Player self, Vector2 from, bool snapup, bool sidesonly)
    {
        if (!GravityHelperModule.ShouldInvertPlayer)
            return orig(self, from, snapup, sidesonly);
        // we need to pretend we've touched the source entity from the opposite side
        var newFrom = new Vector2(from.X, self.Center.Y + (self.Center.Y - from.Y));
        var rv = orig(self, newFrom, snapup, sidesonly);
        // and then invert the vector back on return
        return new Vector2(rv.X, -rv.Y);
    }

    private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playsfx)
    {
        if (self.StateMachine.State != Player.StClimb)
            handleInversionBlocks(self);
        orig(self, particles, playsfx);
    }

    private static bool handleInversionBlocks(Player self)
    {
        foreach (InversionBlock block in self.Scene.Tracker.GetEntities<InversionBlock>())
        {
            if (block.TryHandlePlayer(self))
                return true;
        }

        return false;
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

    private static void Player_ReflectBounce(On.Celeste.Player.orig_ReflectBounce orig, Player self,
        Vector2 direction) =>
        orig(self, GravityHelperModule.ShouldInvertPlayer ? new Vector2(direction.X, -direction.Y) : direction);

    private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
    {
        var scaleY = self.Sprite.Scale.Y;
        var invert = GravityHelperModule.ShouldInvertPlayer;

        if (invert) self.Sprite.Scale.Y = -scaleY;
        orig(self);
        if (invert) self.Sprite.Scale.Y = scaleY;
    }

    private static void Player_StarFlyBegin(On.Celeste.Player.orig_StarFlyBegin orig, Player self)
    {
        orig(self);

        if (GravityHelperModule.ShouldInvertPlayer)
        {
            var bloom = self.starFlyBloom;
            if (bloom != null)
                bloom.Y = Math.Abs(bloom.Y);
        }
    }

    private static void Player_SuperJump(On.Celeste.Player.orig_SuperJump orig, Player self)
    {
        handleInversionBlocks(self);
        orig(self);
    }

    private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target,
        Vector2 direction)
    {
        GravityHelperModule.OverrideSemaphore++;
        bool val = orig(self, target, direction);
        GravityHelperModule.OverrideSemaphore--;
        return val;
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        // cache current inputs
        var feather = Input.Feather.Value;
        var aim = Input.Aim.Value;
        var moveY = Input.MoveY.Value;
        var gliderMoveY = Input.GliderMoveY.Value;
        var level = self.SceneAs<Level>();

        // vvvvvv should be ignored if paused or in cutscene
        if (!level.InCutscene && !level.Paused && self.Scene.GetPersistentController<VvvvvvGravityController>() is { } vvvvvvController)
        {
            // CheckJump() will consume jump and set a buffer time
            vvvvvvController.CheckJump(self);
            // TryFlip() will check Player.onGround to ensure Madeline has been on the ground for at least one frame
            // this ensures her dash and stamina refills
            vvvvvvController.TryFlip(self);
        }

        var shouldInvert = GravityHelperModule.ShouldInvertPlayer;
        var useAbsolute = GravityHelperModule.Settings.ControlScheme == GravityHelperModuleSettings.ControlSchemeSetting.Absolute;
        var useAbsoluteFeather = GravityHelperModule.Settings.FeatherControlScheme == GravityHelperModuleSettings.ControlSchemeSetting.Absolute;

        // invert inputs if we should
        if (shouldInvert)
        {
            if (useAbsolute)
            {
                Input.Aim.Value = new Vector2(aim.X, -aim.Y);
                Input.MoveY.Value = -moveY;
                Input.GliderMoveY.Value = -gliderMoveY;
            }
            if (useAbsoluteFeather)
                Input.Feather.Value = new Vector2(feather.X, -feather.Y);
        }

        // call orig
        orig(self);

        // restore inputs if we should
        if (shouldInvert)
        {
            if (useAbsoluteFeather)
                Input.Feather.Value = feather;
            if (useAbsolute)
            {
                Input.GliderMoveY.Value = gliderMoveY;
                Input.MoveY.Value = moveY;
                Input.Aim.Value = aim;
            }
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

            var ghUdjt = self.Scene.Tracker.GetEntitiesOrEmpty<UpsideDownJumpThru>();
            var mhhUdjt = self.Scene.Tracker.GetEntitiesOrEmpty(ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType);
            var entities = ghUdjt.Concat(mhhUdjt);

            foreach (var entity in entities)
            {
                if (self.CollideCheck(entity) && entity.Bottom - self.Top <= 6f &&
                    !self.DashCorrectCheck(Vector2.UnitY * (entity.Bottom - self.Top)))
                {
                    self.MoveVExact((int)(self.Top - entity.Bottom));
                }
            }

            return true;
        });

        // if gravity is inverted we should skip regular jumpthrus
        cursor.Emit(OpCodes.Brtrue_S, target);
    }

#if REPLACE_LIFTBOOST
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
#endif

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

    private static Lazy<ParticleType> _invertedSummitLandBParticle = new Lazy<ParticleType>(() => new ParticleType(Player.P_SummitLandB)
    {
        Acceleration = Vector2.UnitY * 60f,
    });

    private static Lazy<ParticleType> _invertedSummitLandCParticle = new Lazy<ParticleType>(() => new ParticleType(Player.P_SummitLandC)
    {
        Acceleration = Vector2.UnitY * -20f,
        Direction = (float)Math.PI / 2f,
    });
}
