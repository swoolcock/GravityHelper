// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("Bunneline")]
    public class BunnelineModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private IDetour hook_BunnelineModule_Hair_Render;
        // ReSharper restore InconsistentNaming

        protected override void Load()
        {
            var bmt = ReflectionCache.BunnelineModuleType;
            var bmHairRenderMethod = bmt?.GetMethod("Hair_Render", BindingFlags.Instance | BindingFlags.NonPublic);
            if (bmHairRenderMethod != null)
            {
                hook_BunnelineModule_Hair_Render = new ILHook(bmHairRenderMethod, BunnelineModule_Hair_Render);
            }
        }

        protected override void Unload()
        {
            hook_BunnelineModule_Hair_Render?.Dispose();
            hook_BunnelineModule_Hair_Render = null;
        }

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
