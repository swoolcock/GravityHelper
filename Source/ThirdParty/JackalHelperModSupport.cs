// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty;

[ThirdPartyMod("JackalHelper")]
internal class JackalHelperModSupport : ThirdPartyModSupport
{
    // ReSharper disable once InconsistentNaming
    private static IDetour hook_JackalHelper_CardinalBumper_CardinalLaunch;

    protected override void Load(GravityHelperModule.HookLevel hookLevel)
    {
        var cardinalLaunch = ReflectionCache.JackalHelperCardinalBumper_CardinalLaunch;

        if (cardinalLaunch != null)
            hook_JackalHelper_CardinalBumper_CardinalLaunch = new Hook(cardinalLaunch, GetType().GetMethod(nameof(JackalHelper_CardinalBumper_CardinalLaunch), BindingFlags.NonPublic | BindingFlags.Static)!);
    }

    protected override void Unload()
    {
        hook_JackalHelper_CardinalBumper_CardinalLaunch?.Dispose();
        hook_JackalHelper_CardinalBumper_CardinalLaunch = null;
    }

    private static void JackalHelper_CardinalBumper_CardinalLaunch(Action<Entity, Player, Vector2> orig, Entity self, Player player, Vector2 launchVector)
    {
        // call orig version
        orig(self, player, launchVector);

        // orig will set the player's speed based on the launch vector, so we may need to invert it
        if (GravityHelperModule.ShouldInvertPlayer) player.Speed.Y *= -1;
    }
}
