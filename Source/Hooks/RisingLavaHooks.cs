// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class RisingLavaHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(RisingLava)} hooks...");

        On.Celeste.RisingLava.OnPlayer += RisingLava_OnPlayer;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(RisingLava)} hooks...");

        On.Celeste.RisingLava.OnPlayer -= RisingLava_OnPlayer;
    }

    private static void RisingLava_OnPlayer(On.Celeste.RisingLava.orig_OnPlayer orig, RisingLava self, Player player)
    {
        if (!GravityHelperModule.ShouldInvertPlayer || !SaveData.Instance.Assists.Invincible)
        {
            orig(self, player);
            return;
        }

        if (self.delay > 0f) return;
        var loopSfx = self.loopSfx;

        float from = self.Y;
        float to = self.Y + 48;
        player.Speed.Y = 200f;
        player.RefillDash();
        Tween.Set(self, Tween.TweenMode.Oneshot, 0.4f, Ease.CubeOut, t => self.Y = MathHelper.Lerp(from, to, t.Eased));
        self.delay = 0.5f;
        loopSfx.Param("rising", 0.0f);
        Audio.Play("event:/game/general/assist_screenbottom", player.Position);
    }
}