// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("StaminaMeter")]
    public static class StaminaMeterModSupport
    {
        private const string small_stamina_meter_display_type = "Celeste.Mod.StaminaMeter.SmallStaminaMeterDisplay";

        [HookMethod(small_stamina_meter_display_type, "Render")]
        private static void SmallStaminaMeterDisplay_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchLdfld<Entity>(nameof(Entity.Position))))
                throw new HookException("Couldn't find player.Position");

            cursor.EmitDelegate<Func<Entity, Vector2>>(player =>
            {
                if (!GravityHelperModule.ShouldInvertPlayer) return player.Position;
                return player.BottomCenter + new Vector2(0f, 5f);
            });
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });
    }
}
