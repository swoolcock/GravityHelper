// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
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

            // invert first Speed check
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdflda<Player>(nameof(Player.Speed)),
                instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)),
                instr => instr.MatchLdcR4(0f)))
                throw new HookException("Couldn't find first Speed check.");
            cursor.Index--;
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find first SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // invert second Speed check
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdflda<Player>(nameof(Player.Speed)),
                instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)),
                instr => instr.MatchLdcR4(0f)))
                throw new HookException("Couldn't find second Speed check.");
            cursor.Index--;
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find second SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // cancel the negative
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdflda(ReflectionCache.FrostHelperCustomSpringType, "speedMult"),
                instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)),
                instr => instr.MatchNeg()))
                throw new HookException("Couldn't find neg");
            cursor.Index--;
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });
    }
}
