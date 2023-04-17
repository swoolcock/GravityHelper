// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks;

[HookFixture(typeof(BadelineBoost))]
public static class BadelineBoostHooks {
    [OnHook("OnPlayer", BindingFlags.Instance | BindingFlags.NonPublic)]
    private static void BadelineBoost_OnPlayer(On.Celeste.BadelineBoost.orig_OnPlayer orig, BadelineBoost self, Player player) {
        if (GravityHelperModule.PlayerComponent is { } playerComponent) {
            // update to the expected gravity if custom entity
            if (self is GravityBadelineBoost gravityBadelineBoost)
                playerComponent.SetGravity(gravityBadelineBoost.CurrentDirection, 0f);
            // otherwise force normal gravity to prevent regular badeline boosts from breaking
            else if (playerComponent.CurrentGravity != GravityType.Normal)
                playerComponent.SetGravity(GravityType.Normal, 0f);
        }

        orig(self, player);
    }

    [ILHook("BoostRoutine")]
    private static void BadelineBoost_BoostRoutine(ILContext il) {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(instr => instr.MatchLdfld<BadelineDummy>(nameof(BadelineDummy.Sprite)),
            instr => instr.MatchLdflda<GraphicsComponent>(nameof(GraphicsComponent.Scale))))
            throw new HookException("Couldn't find BadelineDummy.Sprite.Scale");

        // flip Badeline if Madeline is also flipped
        cursor.EmitDelegate<Func<BadelineDummy, BadelineDummy>>(dummy => {
            dummy.SetShouldInvert(GravityHelperModule.ShouldInvertPlayer);
            return dummy;
        });
    }
}
