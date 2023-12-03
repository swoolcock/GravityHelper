// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("ExtendedVariantMode")]
    internal class ExtendedVariantsModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_DashTrailAllTheTime_createTrail;
        private static IDetour hook_JumpIndicator_Render;
        private static IDetour hook_DashCountIndicator_Render;
        // ReSharper restore InconsistentNaming

        protected override void Load(GravityHelperModule.HookLevel hookLevel)
        {
            var dtattt = ReflectionCache.ExtendedVariantsDashTrailAllTheTimeType;
            var createTrailMethod = dtattt?.GetMethod("createTrail", BindingFlags.Static | BindingFlags.NonPublic);
            if (createTrailMethod != null)
            {
                var target = GetType().GetMethod(nameof(DashTrailAllTheTime_createTrail), BindingFlags.Static | BindingFlags.NonPublic);
                hook_DashTrailAllTheTime_createTrail = new Hook(createTrailMethod, target);
            }

            var jit = ReflectionCache.ExtendedVariantsJumpIndicatorType;
            var jiRenderMethod = jit?.GetMethod("Render", BindingFlags.Instance | BindingFlags.Public);
            if (jiRenderMethod != null)
            {
                hook_JumpIndicator_Render = new ILHook(jiRenderMethod, JumpIndicator_Render);
            }

            var dcit = ReflectionCache.ExtendedVariantsDashCountIndicatorType;
            var dciRenderMethod = dcit?.GetMethod("Render", BindingFlags.Instance | BindingFlags.Public);
            if (dciRenderMethod != null)
            {
                hook_DashCountIndicator_Render = new ILHook(dciRenderMethod, DashCountIndicator_Render);
            }
        }

        protected override void Unload()
        {
            hook_DashTrailAllTheTime_createTrail?.Dispose();
            hook_DashTrailAllTheTime_createTrail = null;
            hook_JumpIndicator_Render?.Dispose();
            hook_JumpIndicator_Render = null;
            hook_DashCountIndicator_Render?.Dispose();
            hook_DashCountIndicator_Render = null;
        }

        private static void DashTrailAllTheTime_createTrail(Action<Player> orig, Player player)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
            {
                orig(player);
                return;
            }

            var oldScale = player.Sprite.Scale;
            player.Sprite.Scale.Y = -oldScale.Y;
            orig(player);
            player.Sprite.Scale.Y = oldScale.Y;
        }

        private static void JumpIndicator_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't find vector addition");
            cursor.EmitInvertVectorDelegate();
        });

        private static void DashCountIndicator_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't find vector addition");
            cursor.EmitInvertVectorDelegate();
        });
    }
}
