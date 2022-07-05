// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("ExtendedVariantMode")]
    public static class ExtendedVariantsModSupport
    {
        private const string dash_trail_all_the_time_type = "ExtendedVariants.Variants.DashTrailAllTheTime";
        [ReflectType("ExtendedVariantMode", dash_trail_all_the_time_type)]
        public static Type DashTrailAllTheTimeType;

        private const string jump_indicator_type = "ExtendedVariants.Entities.JumpIndicator";
        [ReflectType("ExtendedVariantMode", jump_indicator_type)]
        public static Type JumpIndicatorType;

        private const string dash_count_indicator_type = "ExtendedVariants.Entities.DashCountIndicator";
        [ReflectType("ExtendedVariantMode", dash_count_indicator_type)]
        public static Type DashCountIndicatorType;

        [ReflectType("ExtendedVariantMode", "ExtendedVariants.Variants.JumpCount")]
        public static Type JumpCountType;

        private static MethodInfo _jumpCountGetJumpBuffer;
        public static MethodInfo JumpCountGetJumpBuffer => _jumpCountGetJumpBuffer ??= JumpCountType?.GetMethod("GetJumpBuffer", BindingFlags.Public | BindingFlags.Static);

        private static MethodInfo _jumpCountSetJumpCount;
        public static MethodInfo JumpCountSetJumpCount => _jumpCountSetJumpCount ??= JumpCountType?.GetMethod("SetJumpCount", BindingFlags.Public | BindingFlags.Static);

        [HookMethod(dash_trail_all_the_time_type, "createTrail")]
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

        [HookMethod(jump_indicator_type, "Render")]
        private static void JumpIndicator_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't find vector addition");
            cursor.EmitInvertVectorDelegate();
        });

        [HookMethod(dash_count_indicator_type, "Render")]
        private static void DashCountIndicator_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.AdditionPredicate))
                throw new HookException("Couldn't find vector addition");
            cursor.EmitInvertVectorDelegate();
        });
    }
}
