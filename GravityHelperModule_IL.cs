using System;
using Celeste;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule
    {
        private static IDetour hook_Player_orig_Update;
        private static IDetour hook_Player_orig_UpdateSprite;

        private static void loadILHooks()
        {
            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
            IL.Celeste.Player.ClimbCheck += Player_ClimbCheck;
            IL.Celeste.Player.ClimbHopBlockedCheck += Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            IL.Celeste.Player.OnCollideV += Player_OnCollideV;
            IL.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;

            hook_Player_orig_Update = new ILHook(playerOrigUpdateMethodInfo, Player_orig_Update);
            hook_Player_orig_UpdateSprite = new ILHook(updateSpriteMethodInfo, Player_orig_UpdateSprite);
        }

        private static void unloadILHooks()
        {
            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
            IL.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
            IL.Celeste.Player.ClimbHopBlockedCheck -= Player_ClimbHopBlockedCheck;
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
            IL.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;

            hook_Player_orig_Update?.Dispose();
            hook_Player_orig_Update = null;

            hook_Player_orig_UpdateSprite?.Dispose();
            hook_Player_orig_UpdateSprite = null;
        }

        private static void Actor_MoveVExact(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Actor, bool>>(a =>
                a is Player player
                && player.CurrentBooster == null
                && !solidMoving
                && !transitioning
                && ShouldInvert);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Starg, 1);
        }

        private static void Actor_IsRiding(ILContext il) => new ILCursor(il).ReplaceAdditionWithDelegate();

        private static void Player_orig_Update(ILContext il)
        {
            var cursor = new ILCursor(il);

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            cursor.ReplaceAdditionWithDelegate(2);

            // Math.Min(base.Y, highestAirY) -> Math.Max(base.Y, highestAirY)
            cursor.ReplaceMaxWithDelegate();

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            cursor.ReplaceAdditionWithDelegate(2);
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
            cursor.GotoNext(MoveType.After, instr => ILExtensions.UnitYPredicate(instr) && ILExtensions.SubtractionPredicate(instr.Next));
            cursor.ReplaceSubtractionWithDelegate();

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il) => new ILCursor(il).ReplaceSubtractionWithDelegate();

        private static void Player_BeforeDownTransition(ILContext il)
        {
            // replaceMaxWithDelegate(new ILCursor(il));
            var cursor = new ILCursor(il);
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_BeforeUpTransition(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdcR4(-105f));
            cursor.Remove();
            cursor.EmitDelegate<Func<float>>(() => !ShouldInvert ? -105f : 105f);
        }

        private static void Solid_GetPlayerOnTop(ILContext il) => new ILCursor(il).ReplaceSubtractionWithDelegate();

        private static void PlayerHair_AfterUpdate(ILContext il)
        {
            // FIXME
            // var cursor = new ILCursor(il);
            // cursor.ReplaceAdditionWithDelegate(-1);
            // cursor.Goto(0);
            // cursor.ReplaceSubtractionWithDelegate(-1);
        }

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

        private static void PlayerOnDreamDashCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            // DreamBlock dreamBlock = this.CollideFirst<DreamBlock>(this.Position + dir);
            cursor.ReplaceAdditionWithDelegate();
            // if (this.CollideCheck<Solid, DreamBlock>(this.Position + dir))
            cursor.ReplaceAdditionWithDelegate();
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            cursor.ReplaceAdditionWithDelegate(2);
            // this.Position = this.Position + vector2 * (float) index;
            cursor.ReplaceAdditionWithDelegate();
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            cursor.ReplaceAdditionWithDelegate(2);
            // this.Position = this.Position + vector2 * (float) index;
            cursor.ReplaceAdditionWithDelegate();
        }

        private static void Player_OnCollideV(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
            cursor.ReplaceSignWithDelegate();
            // this.ReflectBounce(new Vector2(0.0f, (float) -Math.Sign(this.Speed.Y)));
            cursor.ReplaceSignWithDelegate();
            // if (this.DreamDashCheck(Vector2.UnitY * (float) Math.Sign(this.Speed.Y)))
            cursor.ReplaceSignWithDelegate();

            cursor.GotoNext(instr => instr.MatchCall<Entity>(nameof(Entity.CollideCheck)));
            cursor.Goto(cursor.Index - 2);
            cursor.ReplaceAdditionWithDelegate(4);
        }
    }
}