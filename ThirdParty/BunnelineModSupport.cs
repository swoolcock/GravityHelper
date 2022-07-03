// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("Bunneline")]
    public static class BunnelineModSupport
    {
        private const string bunneline_module_type = "Celeste.Mod.Bunneline.BunnelineModule";

        [HookMethod(bunneline_module_type, "Hair_Render")]
        private static void BunnelineModule_Hair_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-3f)))
                throw new HookException("Couldn't invert -3f");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-5f)))
                throw new HookException("Couldn't invert -5f");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.6f), instr => instr.MatchLdcR4(0.6f)))
                throw new HookException("Couldn't invert scale");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.6f), instr => instr.MatchLdcR4(0.6f)))
                throw new HookException("Couldn't invert scale");
            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall("Celeste.PlayerHairExt", "GetHairScale")))
                throw new HookException("Couldn't find first GetHairScale");
            PlayerHairHooks.EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_2);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall("Celeste.PlayerHairExt", "GetHairScale")))
                throw new HookException("Couldn't find first GetHairScale");
            PlayerHairHooks.EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_2);
        });
    }
}
