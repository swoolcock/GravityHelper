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
    [HookFixture("MaddyCrown")]
    public static class MaddyCrownModSupport
    {
        private const string maddy_crown_module_type = "Celeste.Mod.MaddyCrown.MaddyCrownModule";

        [HookMethod(maddy_crown_module_type, "Player_Update")]
        private static void MaddyCrownModule_Player_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchStfld<GraphicsComponent>(nameof(GraphicsComponent.Position))))
                throw new HookException("Couldn't patch MaddyCrownModule.Player_Update");

            cursor.EmitDelegate<Action<GraphicsComponent, Vector2>>((sprite, pos) =>
            {
                sprite.Position = new Vector2(pos.X, Math.Abs(pos.Y) * (GravityHelperModule.ShouldInvertPlayer ? 1 : -1));
                sprite.Scale.Y = GravityHelperModule.ShouldInvertPlayer ? -1 : 1;
            });
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });
    }
}
