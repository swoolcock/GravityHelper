// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("OutbackHelper")]
    public class OutbackHelperModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_OutbackHelper_Portal_OnPlayer;

        protected override void Load()
        {
            var ohpt = ReflectionCache.OutbackHelperPortalType;
            var onPlayerMethod = ohpt?.GetMethod("OnPlayer", BindingFlags.Instance | BindingFlags.NonPublic);

            if (onPlayerMethod != null)
                hook_OutbackHelper_Portal_OnPlayer = new Hook(onPlayerMethod, GetType().GetMethod(nameof(OutbackHelper_Portal_OnPlayer), BindingFlags.NonPublic | BindingFlags.Static)!);
        }

        protected override void Unload()
        {
            hook_OutbackHelper_Portal_OnPlayer?.Dispose();
            hook_OutbackHelper_Portal_OnPlayer = null;
        }

        private static void OutbackHelper_Portal_OnPlayer(Action<Entity, Player> orig, Entity self, Player player)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
            {
                orig(self, player);
                return;
            }

            // reset and re-invert gravity for the player, making sure we don't trigger any controller events
            // using SetGravity ensures that the hitbox and position will be what the portal expects
            GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Normal);
            orig(self, player);
            GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Inverted);
        }
    }
}
