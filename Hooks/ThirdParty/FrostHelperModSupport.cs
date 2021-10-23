// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("FrostHelper")]
    public class FrostHelperModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_FrostHelper_CustomSpring_OnCollide;

        protected override void Load()
        {
            var fhcst = ReflectionCache.FrostHelperCustomSpringType;
            var onCollideMethod = fhcst?.GetMethod("OnCollide", BindingFlags.Instance | BindingFlags.NonPublic);

            if (onCollideMethod != null)
                hook_FrostHelper_CustomSpring_OnCollide = new ILHook(onCollideMethod, FrostHelper_CustomSpring_OnCollide);
        }

        protected override void Unload()
        {
            hook_FrostHelper_CustomSpring_OnCollide?.Dispose();
            hook_FrostHelper_CustomSpring_OnCollide = null;
        }

        private static void FrostHelper_CustomSpring_OnCollide(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert first Speed.Y check
            if (!cursor.TryGotoNext(instr => instr.MatchLdcR4(0), instr => instr.MatchBltUn(out _)))
                throw new HookException("Couldn't find first Speed.Y check.");
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find first SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // invert second Speed.Y check
            if (!cursor.TryGotoNext(instr => instr.MatchLdcR4(0), instr => instr.MatchBgtUn(out _)))
                throw new HookException("Couldn't find second Speed.Y check.");
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find second SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // cancel the negative
            if (!cursor.TryGotoNext(instr => instr.MatchNeg()))
                throw new HookException("Couldn't find neg");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });
    }
}
