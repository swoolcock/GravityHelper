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
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule
    {
        private static IDetour hook_Player_orig_Update;
        private static IDetour hook_Player_orig_UpdateSprite;
        private static IDetour hook_Player_DashCoroutine;

        private static void loadILHooks()
        {
            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding;
            IL.Celeste.Level.EnforceBounds += Level_EnforceBounds;
            IL.Celeste.Player._IsOverWater += Player_IsOverWater;
            IL.Celeste.Player.Bounce += Player_Bounce;
            IL.Celeste.Player.ClimbCheck += Player_ClimbCheck;
            IL.Celeste.Player.ClimbHopBlockedCheck += Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.ExplodeLaunch_Vector2_bool_bool += Player_ExplodeLaunch_Vector2_bool_bool;
            IL.Celeste.Player.Jump += Player_Jump;
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            IL.Celeste.Player.OnCollideH += Player_OnCollideH;
            IL.Celeste.Player.OnCollideV += Player_OnCollideV;
            IL.Celeste.Player.SideBounce += Player_SideBounce;
            IL.Celeste.Player.StarFlyUpdate += Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce += Player_SuperBounce;
            IL.Celeste.Player.SwimCheck += Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck += Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck += Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck += Player_SwimUnderwaterCheck;
            IL.Celeste.PlayerHair.Render += PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;

            hook_Player_orig_Update = new ILHook(ReflectionCache.PlayerOrigUpdateMethodInfo, Player_orig_Update);
            hook_Player_orig_UpdateSprite = new ILHook(ReflectionCache.UpdateSpriteMethodInfo, Player_orig_UpdateSprite);
            hook_Player_DashCoroutine = new ILHook(ReflectionCache.PlayerDashCoroutineMethodInfo.GetStateMachineTarget(), Player_DashCoroutine);
        }

        private static void unloadILHooks()
        {
            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
            IL.Celeste.Player._IsOverWater -= Player_IsOverWater;
            IL.Celeste.Player.Bounce -= Player_Bounce;
            IL.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
            IL.Celeste.Player.ClimbHopBlockedCheck -= Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.ExplodeLaunch_Vector2_bool_bool -= Player_ExplodeLaunch_Vector2_bool_bool;
            IL.Celeste.Player.Jump -= Player_Jump;
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            IL.Celeste.Player.OnCollideH -= Player_OnCollideH;
            IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
            IL.Celeste.Player.SideBounce -= Player_SideBounce;
            IL.Celeste.Player.StarFlyUpdate -= Player_StarFlyUpdate;
            IL.Celeste.Player.SuperBounce -= Player_SuperBounce;
            IL.Celeste.Player.SwimCheck -= Player_SwimCheck;
            IL.Celeste.Player.SwimJumpCheck -= Player_SwimJumpCheck;
            IL.Celeste.Player.SwimRiseCheck -= Player_SwimRiseCheck;
            IL.Celeste.Player.SwimUnderwaterCheck -= Player_SwimUnderwaterCheck;
            IL.Celeste.PlayerHair.Render -= PlayerHair_Render;
            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;

            hook_Player_orig_Update?.Dispose();
            hook_Player_orig_Update = null;

            hook_Player_orig_UpdateSprite?.Dispose();
            hook_Player_orig_UpdateSprite = null;

            hook_Player_DashCoroutine?.Dispose();
            hook_Player_DashCoroutine = null;
        }

        private static void Player_IsOverWater(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdloc(0));
            cursor.EmitDelegate<Func<Rectangle, Rectangle>>(r =>
            {
                if (ShouldInvert) r.Y -= 2;
                return r;
            });
        }

        private static void Player_SwimUnderwaterCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_SwimRiseCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_SwimJumpCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_SwimCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void PlayerHair_Render(ILContext il)
        {
            var cursor = new ILCursor(il);

            void emitChangePositionDelegate()
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Vector2, PlayerHair, Vector2>>((v, hair) =>
                {
                    if (ShouldInvert && hair.Entity is Player player)
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

        private static void Player_StarFlyUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            // this.level.Particles.Emit(FlyFeather.P_Flying, 1, this.Center, Vector2.One * 2f, (-this.Speed).Angle());
            cursor.GotoNext(instr => instr.MatchCallvirt<ParticleSystem>(nameof(ParticleSystem.Emit)));
            cursor.Index--;
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_DashCoroutine(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (player.onGround && (double) player.DashDir.X != 0.0 && ((double) player.DashDir.Y > 0.0 && (double) player.Speed.Y > 0.0) && (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
            cursor.ReplaceAdditionWithDelegate();

            // SlashFx.Burst(player.Center, player.DashDir.Angle());
            cursor.GotoNext(instr => instr.MatchCall<SlashFx>(nameof(SlashFx.Burst)));
            cursor.Index--;
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_ExplodeLaunch_Vector2_bool_bool(ILContext il)
        {
            var cursor = new ILCursor(il);

            // Vector2 vector2 = (this.Center - from).SafeNormalize(-Vector2.UnitY);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_UnaryNegation"));
            cursor.Index += 2;
            cursor.EmitInvertVectorDelegate();
        }

        private static void Player_SideBounce(ILContext il)
        {
            var cursor = new ILCursor(il);

            // this.MoveV(Calc.Clamp(fromY - this.Bottom, -4f, 4f));
            cursor.ReplaceBottomWithDelegate();
        }

        private static void Player_SuperBounce(ILContext il)
        {
            var cursor = new ILCursor(il);

            // this.MoveV(fromY - this.Bottom);
            cursor.ReplaceBottomWithDelegate();
        }

        private static void Player_Bounce(ILContext il)
        {
            var cursor = new ILCursor(il);

            // this.MoveVExact((int) ((double) fromY - (double) this.Bottom));
            cursor.ReplaceBottomWithDelegate();
        }

        private static void Player_OnCollideH(ILContext il)
        {
            var cursor = new ILCursor(il);

            // (SKIP) if (this.onGround && this.DuckFreeAt(this.Position + Vector2.UnitX * (float) Math.Sign(this.Speed.X)))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.DuckFreeAt)));

            // if (!this.CollideCheck<Solid>(this.Position + new Vector2((float) Math.Sign(this.Speed.X), (float) (index1 * index2))))
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Level_EnforceBounds(ILContext il)
        {
            var cursor = new ILCursor(il);

            // else if ((double) player.Bottom > (double) bounds.Bottom && this.Session.MapData.CanTransitionTo(this, player.Center + Vector2.UnitY * 12f) && !this.Session.LevelData.DisableDownTransition)
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.BeforeDownTransition)));
            cursor.GotoPrev(Extensions.BottomPredicate);
            cursor.Remove();
            cursor.EmitDelegate(Extensions.BottomDelegate);
        }

        private static void Player_Jump(ILContext il)
        {
            var cursor = new ILCursor(il);

            // Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(this.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
            cursor.ReplaceAdditionWithDelegate();

            // Dust.Burst(this.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
            cursor.ReplaceBottomCenterWithDelegate();
        }

        private static void Actor_IsRiding(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_orig_Update(ILContext il)
        {
            var cursor = new ILCursor(il);

            // Platform platform = (Platform) this.CollideFirst<Solid>(this.Position + Vector2.UnitY) ?? (Platform) this.CollideFirstOutside<JumpThru>(this.Position + Vector2.UnitY);
            cursor.ReplaceAdditionWithDelegate(2);

            // this.highestAirY = !this.onGround ? Math.Min(this.Y, this.highestAirY) : this.Y;
            cursor.ReplaceMinWithDelegate();

            // else if (this.onGround && (this.CollideCheck<Solid, NegaBlock>(this.Position + Vector2.UnitY) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY)) && (!this.CollideCheck<Spikes>(this.Position) || SaveData.Instance.Assists.Invincible))
            cursor.ReplaceAdditionWithDelegate(2);

            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) Math.Sign(this.wallSpeedRetained)))
            cursor.GotoNextAddition(MoveType.After);

            // (SKIP) else if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float) this.hopWaitX))
            cursor.GotoNextAddition(MoveType.After);

            // if (!this.onGround && this.DashAttacking && (double) this.DashDir.Y == 0.0 && (this.CollideCheck<Solid>(this.Position + Vector2.UnitY * 3f) || this.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY * 3f)))
            cursor.ReplaceAdditionWithDelegate(2);

            // invert Center.Y check (fixes Madeline slamming into the ground when climbing down into water)
            // if (water != null && (double) this.Center.Y < (double) water.Center.Y)
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitDelegate<Func<float, float>>(f => ShouldInvert ? -f : f);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitDelegate<Func<float, float>>(f => ShouldInvert ? -f : f);
        }

        private static void Player_orig_UpdateSprite(ILContext il)
        {
            var cursor = new ILCursor(il);

            // fix dangling animation
            cursor.ReplaceAdditionWithDelegate();

            // skip push check
            cursor.GotoNextAddition(MoveType.After);

            // fix edge animation
            cursor.ReplaceAdditionWithDelegate(3);

            // fix edgeBack animation
            cursor.ReplaceAdditionWithDelegate(3);
        }

        private static void Player_ClimbUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
            cursor.GotoNext(MoveType.After, instr => Extensions.UnitYPredicate(instr) && Extensions.SubtractionPredicate(instr.Next));
            cursor.ReplaceSubtractionWithDelegate();

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il) => new ILCursor(il).ReplaceSubtractionWithDelegate();

        private static void Solid_GetPlayerOnTop(ILContext il) => new ILCursor(il).ReplaceSubtractionWithDelegate();

        private static void Player_ClimbCheck(ILContext il)
        {
            var cursor = new ILCursor(il);

            // replace Y
            cursor.ReplaceAdditionWithDelegate();

            // skip X
            cursor.GotoNextAddition(MoveType.After);

            // replace Y
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_NormalUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float) -index) && this.ClimbCheck((int) this.Facing, -index))
            cursor.GotoNextUnitY(MoveType.After);
            cursor.ReplaceAdditionWithDelegate();

            // if ((water = this.CollideFirst<Water>(this.Position + Vector2.UnitY * 2f)) != null)
            cursor.GotoNextUnitY(MoveType.After);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_OnCollideV(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
            cursor.ReplaceSignWithDelegate();

            cursor.GotoNext(instr => instr.MatchCall<Entity>(nameof(Entity.CollideCheck)));
            cursor.Goto(cursor.Index - 2);
            cursor.ReplaceAdditionWithDelegate(4);
        }
    }
}