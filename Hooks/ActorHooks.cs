using System;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class ActorHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Actor)} hooks...");

            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding_JumpThru;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding_Solid;
            IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;

            On.Celeste.Actor.MoveV += Actor_MoveV;
            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Actor)} hooks...");

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding_JumpThru;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding_Solid;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;

            On.Celeste.Actor.MoveV -= Actor_MoveV;
            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
        }

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
    }
}
