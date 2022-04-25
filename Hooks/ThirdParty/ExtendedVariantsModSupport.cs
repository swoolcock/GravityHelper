// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("ExtendedVariantMode")]
    public class ExtendedVariantsModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_DashTrailAllTheTime_createTrail;

        protected override void Load()
        {
            var dtattt = ReflectionCache.DashTrailAllTheTimeType;
            var createTrailMethod = dtattt?.GetMethod("createTrail", BindingFlags.Static | BindingFlags.NonPublic);

            if (createTrailMethod != null)
            {
                var target = GetType().GetMethod(nameof(DashTrailAllTheTime_createTrail), BindingFlags.Static | BindingFlags.NonPublic);
                hook_DashTrailAllTheTime_createTrail = new Hook(createTrailMethod, target);
            }
        }

        protected override void Unload()
        {
            hook_DashTrailAllTheTime_createTrail?.Dispose();
            hook_DashTrailAllTheTime_createTrail = null;
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
    }
}
