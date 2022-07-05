// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Monocle;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("OutbackHelper")]
    public static class OutbackHelperModSupport
    {
        private const string portal_type = "Celeste.Mod.OutbackHelper.Portal";

        [HookMethod(portal_type, "OnPlayer")]
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
