// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class RisingLavaHooks
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

            var data = DynamicData.For(self);
            if (data.Get<float>("delay") > 0f) return;
            var loopSfx = data.Get<SoundSource>("loopSfx");

            float from = self.Y;
            float to = self.Y + 48;
            player.Speed.Y = 200f;
            player.RefillDash();
            Tween.Set(self, Tween.TweenMode.Oneshot, 0.4f, Ease.CubeOut, t => self.Y = MathHelper.Lerp(from, to, t.Eased));
            data.Set("delay", 0.5f);
            loopSfx.Param("rising", 0.0f);
            Audio.Play("event:/game/general/assist_screenbottom", player.Position);
        }
    }
}
