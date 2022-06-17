// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("StaminaMeter")]
    public class StaminaMeterModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private IDetour hook_SmallStaminaMeterDisplay_Render;

        protected override void Load()
        {
            var ssmdt = ReflectionCache.StaminaMeterSmallStaminaMeterDisplayType;
            var renderMethod = ssmdt?.GetMethod("Render", BindingFlags.Public | BindingFlags.Instance);
            if (renderMethod != null)
            {
                hook_SmallStaminaMeterDisplay_Render = new ILHook(renderMethod, SmallStaminaMeterDisplay_Render);
            }
        }

        protected override void Unload()
        {
            hook_SmallStaminaMeterDisplay_Render?.Dispose();
            hook_SmallStaminaMeterDisplay_Render = null;
        }

        private void SmallStaminaMeterDisplay_Render(ILContext il) => HookUtils.SafeHook(() =>
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
