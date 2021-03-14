using System;
using Celeste;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule
    {
        private delegate Vector2 VectorBinaryOperation(Vector2 lhs, Vector2 rhs);
        private delegate float FloatBinaryOperation(float a, float b);
        private delegate int FloatUnaryOperation(float a);

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

        private static void Actor_IsRiding(ILContext il) => replaceAdditionWithDelegate(new ILCursor(il));

        private static void Player_orig_Update(ILContext il)
        {
            var cursor = new ILCursor(il);
            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor, 2);

            // Math.Min(base.Y, highestAirY) -> Math.Max(base.Y, highestAirY)
            replaceMaxWithDelegate(cursor);

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor, 2);
        }

        private static void Player_orig_UpdateSprite(ILContext il)
        {
            var cursor = new ILCursor(il);

            // fix dangling animation
            replaceAdditionWithDelegate(cursor);

            // skip push check
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("op_Addition"));

            // fix edge animation
            replaceAdditionWithDelegate(cursor, 3);

            // fix edgeBack animation
            replaceAdditionWithDelegate(cursor, 3);
        }

        private static void Player_ClimbUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
            cursor.GotoNext(MoveType.After,
                instr => instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Subtraction"));
            replaceSubtractionWithDelegate(cursor);

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            replaceAdditionWithDelegate(cursor);
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            replaceSubtractionWithDelegate(cursor);
        }

        private static void Player_BeforeDownTransition(ILContext il) => replaceMaxWithDelegate(new ILCursor(il));

        private static void Player_BeforeUpTransition(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdcR4(-105f));
            cursor.Remove();
            cursor.EmitDelegate<Func<float>>(() => !ShouldInvert ? -105f : 105f);
        }

        private static void Solid_GetPlayerOnTop(ILContext il) => replaceSubtractionWithDelegate(new ILCursor(il));

        private static void PlayerHair_AfterUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            replaceAdditionWithDelegate(cursor, -1);
            cursor.Goto(0);
            replaceSubtractionWithDelegate(cursor, -1);
        }

        private static void Player_ClimbCheck(ILContext il)
        {
            var cursor = new ILCursor(il);

            // replace Y
            replaceAdditionWithDelegate(cursor);

            // skip X
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("op_Addition"));

            // replace Y
            replaceAdditionWithDelegate(cursor);
        }

        private static void Player_NormalUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (!this.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float) -index) && this.ClimbCheck((int) this.Facing, -index))
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            replaceAdditionWithDelegate(cursor);

            // if ((water = this.CollideFirst<Water>(this.Position + Vector2.UnitY * 2f)) != null)
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY"));
            replaceAdditionWithDelegate(cursor);
        }

        private static void PlayerOnDreamDashCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            // DreamBlock dreamBlock = this.CollideFirst<DreamBlock>(this.Position + dir);
            replaceAdditionWithDelegate(cursor);
            // if (this.CollideCheck<Solid, DreamBlock>(this.Position + dir))
            replaceAdditionWithDelegate(cursor);
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            replaceAdditionWithDelegate(cursor, 2);
            // this.Position = this.Position + vector2 * (float) index;
            replaceAdditionWithDelegate(cursor);
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            replaceAdditionWithDelegate(cursor, 2);
            // this.Position = this.Position + vector2 * (float) index;
            replaceAdditionWithDelegate(cursor);
        }

        private static void Player_OnCollideV(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
            replaceSignWithDelegate(cursor);
            // this.ReflectBounce(new Vector2(0.0f, (float) -Math.Sign(this.Speed.Y)));
            replaceSignWithDelegate(cursor);
            // if (this.DreamDashCheck(Vector2.UnitY * (float) Math.Sign(this.Speed.Y)))
            replaceSignWithDelegate(cursor);

            cursor.GotoNext(instr => instr.MatchCall<Entity>(nameof(Entity.CollideCheck)));
            cursor.Goto(cursor.Index - 2);
            replaceAdditionWithDelegate(cursor, 4);
        }

        private static void replaceAdditionWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall<Vector2>("op_Addition")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) =>
                    lhs + (ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs));
            }
        }

        private static void replaceSubtractionWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) =>
                    lhs - (ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs));
            }
        }

        private static void replaceMaxWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall("System.Math", "Max")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<FloatBinaryOperation>((a, b) =>
                    ShouldInvert ? Math.Min(a, b) : Math.Max(a, b));
            }
        }

        private static void replaceSignWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall("System.Math", "Sign")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<FloatUnaryOperation>(a =>
                    ShouldInvert ? -Math.Sign(a) : Math.Sign(a));
            }
        }
    }
}