// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
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

            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding_Solid;
            IL.Celeste.Actor.OnGround_int += Actor_OnGround_int;

            // we need to run this after MaxHelpingHand to ensure both UDJT types are handled
            using (new DetourContext {After = {"MaxHelpingHand"}})
                IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;

            On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding_JumpThru;
        }


        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Actor)} hooks...");

            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding_Solid;
            IL.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;

            On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding_JumpThru;
        }

        private static bool Actor_IsRiding_JumpThru(On.Celeste.Actor.orig_IsRiding_JumpThru orig, Actor self, JumpThru jumpThru)
        {
            // we override all other hooks, since it's the only way to accurately handle riding UDJT
            if (self.IgnoreJumpThrus) return false;
            var shouldInvert = self.ShouldInvertChecked();
            return shouldInvert && jumpThru.IsUpsideDownJumpThru() &&
                self.CollideCheckOutside(jumpThru, self.Position - Vector2.UnitY) ||
                !shouldInvert && !jumpThru.IsUpsideDownJumpThru() &&
                self.CollideCheckOutside(jumpThru, self.Position + Vector2.UnitY);
        }

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

            // handle standing on upside-down jumpthrus
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Actor, int, JumpThru>>((self, moveV) =>
            {
                if (moveV >= 0 || self.IgnoreJumpThrus) return null;
                return self.CollideFirstOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY);
            });
            cursor.Emit(OpCodes.Stloc, variable);
            cursor.Emit(OpCodes.Ldloc, variable);
            cursor.Emit(OpCodes.Brtrue, cursor2.Next);

            // handle standing on regular jumpthrus (ignore UDJT)
            if (!cursor.TryGotoNext(instr => instr.MatchLdloc(variable.Index),
                instr => instr.MatchBrfalse(out _)))
                throw new HookException("Couldn't find if (platform != null)");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, variable);
            cursor.EmitDelegate<Func<Actor, int, Platform, Platform>>((self, moveV, platform) =>
            {
                // without this, Madeline will land on UDJT while regular gravity
                if (platform is JumpThru jumpThru && jumpThru.IsUpsideDownJumpThru())
                    return self.CollideFirstOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY * Math.Sign(moveV));
                return platform;
            });
            cursor.Emit(OpCodes.Stloc, variable);
        });

        private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher) =>
            orig(self,
                GravityHelperModule.OverrideSemaphore <= 0 &&
                self.ShouldInvertChecked() ? -moveV : moveV, onCollide, pusher);

        private static void Actor_OnGround_int(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1)))
                throw new HookException("Couldn't find first downCheck");
            cursor.EmitActorInvertIntDelegate(OpCodes.Ldarg_0);

            if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0), instr => instr.MatchLdarg(0)))
                throw new HookException("Couldn't find CollideCheckOutside<JumpThru>");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Entity, int, bool>>((self, downCheck) => self.ShouldInvertChecked()
                ? self.CollideCheckOutsideUpsideDownJumpThru(self.Position - Vector2.UnitY * downCheck)
                : self.CollideCheckOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY * downCheck));
            cursor.Emit(OpCodes.Ret);
        });
    }
}
