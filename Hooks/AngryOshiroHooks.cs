// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class AngryOshiroHooks
{
    // ReSharper disable UnusedMember.Local
    private const int StChase = 0;
    private const int StChargeUp = 1;
    private const int StAttack = 2;
    private const int StDummy = 3;
    private const int StWaiting = 4;
    private const int StHurt = 5;
    // ReSharper restore UnusedMember.Local

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(AngryOshiro)} hooks...");
        On.Celeste.AngryOshiro.ctor_Vector2_bool += AngryOshiro_ctor_Vector2_bool;
        On.Celeste.AngryOshiro.OnPlayerBounce += AngryOshiro_OnPlayerBounce;
        On.Celeste.AngryOshiro.HurtUpdate += AngryOshiro_HurtUpdate;
        On.Celeste.AngryOshiro.Render += AngryOshiro_Render;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(AngryOshiro)} hooks...");
        On.Celeste.AngryOshiro.ctor_Vector2_bool -= AngryOshiro_ctor_Vector2_bool;
        On.Celeste.AngryOshiro.OnPlayerBounce -= AngryOshiro_OnPlayerBounce;
        On.Celeste.AngryOshiro.HurtUpdate -= AngryOshiro_HurtUpdate;
        On.Celeste.AngryOshiro.Render -= AngryOshiro_Render;
    }

    private static void AngryOshiro_ctor_Vector2_bool(On.Celeste.AngryOshiro.orig_ctor_Vector2_bool orig, AngryOshiro self, Vector2 position, bool fromCutscene)
    {
        orig(self, position, fromCutscene);

        var bounceCollider = self.bounceCollider;
        var normalBounceColliderTop = bounceCollider.Collider.Top;
        var colliderOffset = self.Collider.Top - normalBounceColliderTop;
        var invertedBounceColliderBottom = self.Collider.Bottom + colliderOffset;

        self.Add(new PlayerGravityListener((player, args) =>
        {
            if (args.NewValue == GravityType.Normal)
                bounceCollider.Collider.Top = normalBounceColliderTop;
            else if (args.NewValue == GravityType.Inverted)
                bounceCollider.Collider.Bottom = invertedBounceColliderBottom;
        }));
    }

    private static void AngryOshiro_OnPlayerBounce(On.Celeste.AngryOshiro.orig_OnPlayerBounce orig, AngryOshiro self, Player player)
    {
        if (!GravityHelperModule.ShouldInvertPlayer)
        {
            orig(self, player);
            return;
        }

        var state = self.state;

        if (state.State != StAttack || player.Top < self.Bottom - 6)
            return;

        self.SetShouldInvert(true);

        var prechargeSfx = self.prechargeSfx;
        var chargeSfx = self.chargeSfx;
        Audio.Play("event:/game/general/thing_booped", self.Position);
        Celeste.Freeze(0.2f);
        player.Bounce(self.Bottom - 2f);
        state.State = StHurt;
        prechargeSfx.Stop();
        chargeSfx.Stop();
    }

    private static int AngryOshiro_HurtUpdate(On.Celeste.AngryOshiro.orig_HurtUpdate orig, AngryOshiro self)
    {
        if (!self.ShouldInvert())
            return orig(self);

        self.X += 100f * Engine.DeltaTime;
        self.Y -= 200f * Engine.DeltaTime;

        if (self.Bottom >= (double) (self.SceneAs<Level>().Bounds.Top - 20))
            return StHurt;

        self.SetShouldInvert(false);

        if (self.leaving)
        {
            self.RemoveSelf();
            return StHurt;
        }

        self.X = self.SceneAs<Level>().Camera.Left - 48f;
        self.cameraXOffset = -48f;
        self.doRespawnAnim = true;
        self.Visible = false;

        return StChase;
    }

    private static void AngryOshiro_Render(On.Celeste.AngryOshiro.orig_Render orig, AngryOshiro self)
    {
        if (!self.ShouldInvert())
        {
            orig(self);
            return;
        }

        var scaleY = self.Sprite.Scale.Y;
        self.Sprite.Scale.Y = -scaleY;
        orig(self);
        self.Sprite.Scale.Y = scaleY;
    }
}
