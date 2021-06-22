using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace GravityHelper
{
    internal static class Extensions
    {
        #region Entity Extensions

        public static T GetEntityOrDefault<T>(this Tracker tracker, Func<T, bool> predicate = null) where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return default;
            var ofType = list.OfType<T>();
            return predicate == null ? ofType.FirstOrDefault() : ofType.FirstOrDefault(predicate);
        }

        public static IEnumerable<T> GetEntitiesOrEmpty<T>(this Tracker tracker, Func<T, bool> predicate = null) where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return Enumerable.Empty<T>();
            var ofType = list.OfType<T>();
            return predicate == null ? ofType : ofType.Where(predicate);
        }

        public static T CollideFirstOrDefault<T>(this Entity entity) where T : Entity =>
            entity.Scene.Tracker.Entities.ContainsKey(typeof(T)) ? entity.CollideFirst<T>() : default;

        public static Entity CollideFirstOutside(this Entity entity, Type type, Vector2 at, bool checkAllEntities = false)
        {
            IEnumerable<Entity> entities = getEntities(entity.Scene, type, checkAllEntities);
            if (entities == null) return null;

            foreach (Entity b in entities)
            {
                if (!Collide.Check(entity, b) && Collide.Check(entity, b, at))
                    return b;
            }

            return default;
        }

        public static bool CollideCheckOutside(this Entity entity, Type type, Vector2 at, bool checkAllEntities = false) =>
            entity.CollideFirstOutside(type, at, checkAllEntities) != null;

        public static Entity CollideFirst(this Entity entity, Type type, Vector2 at, bool checkAllEntities = false)
        {
            IEnumerable<Entity> entities = getEntities(entity.Scene, type, checkAllEntities);
            if (entities == null) return null;

            return Collide.First(entity, entities, at);
        }

        public static bool CollideCheck(this Entity entity, Type type, Vector2 at, bool checkAllEntities = false)
        {
            IEnumerable<Entity> entities = getEntities(entity.Scene, type, checkAllEntities);
            if (entities == null) return false;

            return Collide.Check(entity, entities, at);
        }

        private static IEnumerable<Entity> getEntities(Scene scene, Type type, bool checkAllEntities = false)
        {
            IEnumerable<Entity> entities = null;
            if (checkAllEntities)
                entities = scene.Tracker.Entities.SelectMany(p => p.Value.Where(e => e.GetType() == type));
            else if (scene.Tracker.Entities.ContainsKey(type))
                entities = scene.Tracker.Entities[type];

            return entities;
        }

        #endregion

        #region IL Extensions
        public static bool AdditionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Addition");
        public static bool SubtractionPredicate(Instruction instr) => instr.MatchCall<Vector2>("op_Subtraction");
        public static bool UnitYPredicate(Instruction instr) => instr.MatchCall<Vector2>("get_UnitY");
        public static bool MinPredicate(Instruction instr) => instr.MatchCall("System.Math", "Min");
        public static bool MaxPredicate(Instruction instr) => instr.MatchCall("System.Math", "Max");
        public static bool SignPredicate(Instruction instr) => instr.MatchCall("System.Math", "Sign");
        public static bool BottomPredicate(Instruction instr) => instr.MatchCallOrCallvirt<Entity>("get_Bottom");
        public static bool BottomCenterPredicate(Instruction instr) => instr.MatchCallOrCallvirt<Entity>("get_BottomCenter");

        public static readonly Func<Vector2, Vector2, Vector2> AdditionDelegate = (lhs, rhs) =>
            lhs + (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<Vector2, Vector2, Vector2> SubtractionDelegate = (lhs, rhs) =>
            lhs - (GravityHelperModule.ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs);

        public static readonly Func<float, float, float> MinDelegate = (a, b) =>
            GravityHelperModule.ShouldInvert ? Math.Max(a, b) : Math.Min(a, b);

        public static readonly Func<float, float, float> MaxDelegate = (a, b) =>
            GravityHelperModule.ShouldInvert ? Math.Min(a, b) : Math.Max(a, b);

        public static readonly Func<float, float> SignDelegate = a =>
            GravityHelperModule.ShouldInvert ? -Math.Sign(a) : Math.Sign(a);

        public static readonly Func<Entity, float> BottomDelegate = e =>
            GravityHelperModule.ShouldInvert ? e.Top : e.Bottom;

        public static readonly Func<Entity, Vector2> BottomCenterDelegate = e =>
            GravityHelperModule.ShouldInvert ? e.TopCenter : e.BottomCenter;

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
        public static void ReplaceMinWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(MinPredicate, MinDelegate, count);
        public static void ReplaceMaxWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(MaxPredicate, MaxDelegate, count);
        public static void ReplaceSignWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(SignPredicate, SignDelegate, count);
        public static void ReplaceBottomWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(BottomPredicate, BottomDelegate, count);
        public static void ReplaceBottomCenterWithDelegate(this ILCursor cursor, int count = 1) => cursor.ReplaceWithDelegate(BottomCenterPredicate, BottomCenterDelegate, count);

        public static void GotoNextAddition(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, AdditionPredicate);
        public static void GotoNextSubtraction(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, SubtractionPredicate);
        public static void GotoNextMin(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, MinPredicate);
        public static void GotoNextMax(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, MaxPredicate);
        public static void GotoNextSign(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, SignPredicate);
        public static void GotoNextUnitY(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, UnitYPredicate);
        public static void GotoNextBottom(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, BottomPredicate);
        public static void GotoNextBottomCenter(this ILCursor cursor, MoveType moveType = MoveType.Before) => cursor.GotoNext(moveType, BottomCenterPredicate);

        public static void EmitInvertVectorDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<Vector2, Vector2>>(v => GravityHelperModule.ShouldInvert ? new Vector2(v.X, -v.Y) : v);

        public static void EmitInvertFloatDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<float, float>>(f => GravityHelperModule.ShouldInvert ? -f : f);

        public static void EmitInvertIntDelegate(this ILCursor cursor) =>
            cursor.EmitDelegate<Func<int, int>>(i => GravityHelperModule.ShouldInvert ? -i : i);

        #endregion
    }
}
