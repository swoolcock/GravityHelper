// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("FrostHelper")]
    public static class FrostHelperModSupport
    {
        private const string custom_spring_type = "FrostHelper.CustomSpring";
        [ReflectType("FrostHelper", custom_spring_type)]
        public static Type FrostHelperCustomSpringType;

        [HookMethod(custom_spring_type, "OnCollide")]
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
                instr => instr.MatchLdflda(FrostHelperCustomSpringType, "speedMult"),
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
