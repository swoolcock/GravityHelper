using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace GravityHelper
{
    internal static class ILExtensions
    {
        public static bool AdditionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Addition");
        public static bool SubtractionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Subtraction");
        public static bool UnitYPredicate(Instruction instr) => instr.MatchCall<Vector2>("get_UnitY");
        public static bool MaxPredicate(Instruction instr) => instr.MatchCall("System.Math", "Max");
        public static bool SignPredicate(Instruction instr) => instr.MatchCall("System.Math", "Sign");

        public static readonly Func<Vector2, Vector2, Vector2> AdditionDelegate = (lhs, rhs) =>
            lhs + (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<Vector2, Vector2, Vector2> SubtractionDelegate = (lhs, rhs) =>
            lhs - (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<float, float, float> MaxDelegate = (a, b) =>
            GravityHelperModule.ShouldInvert ? Math.Min(a, b) : Math.Max(a, b);

        public static readonly Func<float, float> SignDelegate = a =>
            GravityHelperModule.ShouldInvert ? -Math.Sign(a) : Math.Sign(a);

        public static void ReplaceWithDelegate<T>(this ILCursor cursor, Func<Instruction, bool> predicate, T del, int count = 1)
            where T : Delegate
        {
            while (count != 0)
            {
                if (count == -1 && !cursor.TryGotoNext(predicate)) break;
                if (count >= 0) cursor.GotoNext(predicate);
                if (count > 0) count--;

                cursor.Remove();
                cursor.EmitDelegate(del);
            }
        }

        public static void ReplaceAdditionWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(AdditionPredicate, AdditionDelegate, count);
        public static void ReplaceSubtractionWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(SubtractionPredicate, SubtractionDelegate, count);
        public static void ReplaceMaxWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(MaxPredicate, MaxDelegate, count);
        public static void ReplaceSignWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(SignPredicate, SignDelegate, count);

        public static void GotoNextAddition(this ILCursor cursor, MoveType moveType) => cursor.GotoNext(moveType, AdditionPredicate);
        public static void GotoNextSubtraction(this ILCursor cursor, MoveType moveType) => cursor.GotoNext(moveType, SubtractionPredicate);
        public static void GotoNextMax(this ILCursor cursor, MoveType moveType) => cursor.GotoNext(moveType, MaxPredicate);
        public static void GotoNextSign(this ILCursor cursor, MoveType moveType) => cursor.GotoNext(moveType, SignPredicate);
        public static void GotoNextUnitY(this ILCursor cursor, MoveType moveType) => cursor.GotoNext(moveType, UnitYPredicate);
    }
}