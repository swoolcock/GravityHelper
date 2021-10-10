// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

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
            IL.Celeste.Actor.MoveV += Actor_MoveV;

            // we need to run this after MaxHelpingHand to ensure both UDJT types are handled
            using (new DetourContext {After = {"MaxHelpingHand"}})
                IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;

            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Actor)} hooks...");

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding_JumpThru;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding_Solid;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            IL.Celeste.Actor.MoveV -= Actor_MoveV;

            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
        }

        private static void Actor_IsRiding_JumpThru(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(1),
                instr => instr.MatchLdarg(0)))
                throw new HookException("Couldn't patch Actor.IsRiding for jumpthrus");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Actor, JumpThru, bool>>((self, jumpThru) =>
            {
                var shouldInvert = GravityHelperModule.ShouldInvertActor(self);
                return shouldInvert && jumpThru.IsUpsideDownJumpThru() &&
                       self.CollideCheckOutside(jumpThru, self.Position - Vector2.UnitY) ||
                       !shouldInvert && !jumpThru.IsUpsideDownJumpThru() &&
                       self.CollideCheckOutside(jumpThru, self.Position + Vector2.UnitY);
            });
            cursor.Emit(OpCodes.Ret);
        });

        private static void Actor_IsRiding_Solid(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY")))
                throw new HookException("Couldn't patch Actor.IsRiding for solids");

            cursor.EmitActorInvertVectorDelegate(OpCodes.Ldarg_0);
        });

        private static void Actor_MoveVExact(ILContext il) => HookUtils.SafeHook(() =>
        {
            // borrowed and repurposed some code from MaxHelpingHand to check for GH UDJT
            var cursor = new ILCursor(il);
            VariableDefinition variable = il.Method.Body.Variables.FirstOrDefault(v =>
                v.VariableType.FullName == "Celeste.Platform");

            if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg(1), instr => instr.MatchLdcI4(0)))
                throw new HookException("Couldn't find moveV > 0");

            var cursor2 = cursor.Clone();

            if (!cursor2.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg(0), instr => instr.MatchLdflda<Actor>("movementCounter")))
                throw new HookException("Couldn't find movementCounter");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Actor, int, JumpThru>>((self, moveV) =>
            {
                if (moveV >= 0 || self.IgnoreJumpThrus) return null;
                return self.CollideFirstOutside<UpsideDownJumpThru>(self.Position - Vector2.UnitY);
            });
            cursor.Emit(OpCodes.Stloc, variable);
            cursor.Emit(OpCodes.Ldloc, variable);
            cursor.Emit(OpCodes.Brtrue, cursor2.Next);
        });

        private static void Actor_MoveV(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert moveV at the start of the method if we need to
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Actor, bool>>(GravityHelperModule.ShouldInvertActor);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next.Next);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Starg, 1);

            // and revert it before we call MoveVExact
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchSub(),
                instr => instr.MatchStindR4(),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdloc(0)))
                throw new HookException("Couldn't find call to MoveVExact");

            cursor.EmitActorInvertIntDelegate(OpCodes.Ldarg_0);
        });

        private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher) =>
            orig(self,
                GravityHelperModule.ShouldInvertActor(self) &&
                !GravityHelperModule.SolidMoving &&
                !GravityHelperModule.Transitioning ? -moveV : moveV, onCollide, pusher);

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
        {
            if (!GravityHelperModule.ShouldInvertActor(self))
                return orig(self, downCheck);

            if (self.CollideCheck<Solid>(self.Position - Vector2.UnitY * downCheck))
                return true;

            if (!self.IgnoreJumpThrus)
                return self.CollideCheckOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY * downCheck);

            return false;
        }
    }
}
