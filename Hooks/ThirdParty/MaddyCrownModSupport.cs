// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("MaddyCrown")]
    public class MaddyCrownModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_MaddyCrownModule_Player_Update;

        protected override void Load()
        {
            var mcmt = ReflectionCache.MaddyCrownModuleType;
            var playerUpdateMethod = mcmt?.GetMethod("Player_Update", BindingFlags.Instance | BindingFlags.NonPublic);

            if (playerUpdateMethod != null)
                hook_MaddyCrownModule_Player_Update = new ILHook(playerUpdateMethod, MaddyCrownModule_Player_Update);
        }

        protected override void Unload()
        {
            hook_MaddyCrownModule_Player_Update?.Dispose();
            hook_MaddyCrownModule_Player_Update = null;
        }

        private static void MaddyCrownModule_Player_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchStfld<GraphicsComponent>(nameof(GraphicsComponent.Position))))
                throw new HookException("Couldn't patch MaddyCrownModule.Player_Update");

            cursor.EmitDelegate<Action<GraphicsComponent, Vector2>>((sprite, pos) =>
            {
                sprite.Position = new Vector2(pos.X, Math.Abs(pos.Y) * (GravityHelperModule.ShouldInvert ? 1 : -1));
                sprite.Scale.Y = GravityHelperModule.ShouldInvert ? -1 : 1;
            });
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });
    }
}
