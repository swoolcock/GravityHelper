// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/GravityBooster")]
public class GravityBooster : Booster
{
    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    public GravityType GravityType { get; }

    private readonly Sprite _rippleSprite;
    private readonly Sprite _overlaySprite;

    private string overlayId => GravityType switch
    {
        GravityType.Inverted => "overlay_invert",
        GravityType.Toggle => "overlay_toggle",
        _ => "overlay_normal",
    };

    public GravityBooster(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Bool("red"))
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        GravityType = (GravityType)data.Int("gravityType");

        GFX.SpriteBank.CreateOn(sprite, red ? "gravityBoosterRed" : "gravityBooster");

        Add(_overlaySprite = sprite.CreateClone());
        _overlaySprite.Play(overlayId);

        Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
        _rippleSprite.Play("loop");

        Add(new AccessibilityListener(onAccessibilityChange));
        onAccessibilityChange();
    }

    private void onAccessibilityChange()
    {
        if (_rippleSprite != null) _rippleSprite.Color = GravityType.HighlightColor();
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        fixSprites();
    }

    public override void Update()
    {
        base.Update();
        fixSprites();
    }

    private void fixSprites()
    {
        const float ripple_offset = 5f;
        var currentGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;

        if (GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && currentGravity == GravityType.Normal)
        {
            _rippleSprite.Y = -ripple_offset;
            _rippleSprite.Scale.Y = 1f;
            // change to the correct loop if we need to
            if (sprite.CurrentAnimationID == "loop_down")
                sprite.Play("loop");
        }
        else if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted)
        {
            _rippleSprite.Y = ripple_offset;
            _rippleSprite.Scale.Y = -1f;
            // change to the correct loop if we need to
            if (sprite.CurrentAnimationID == "loop")
                sprite.Play("loop_down");
        }

        if (GravityType == GravityType.Toggle)
        {
            _overlaySprite.Scale.Y = currentGravity == GravityType.Normal ? -1 : 1;
        }

        _rippleSprite.Visible = _overlaySprite.Visible = sprite.CurrentAnimationID.StartsWith("loop");
    }

    public override void Render()
    {
        if (GravityHelperAPI.Exports.BeginCustomTintShader())
        {
            var oldSpriteColor = sprite.Color;
            var oldOverlayColor = _overlaySprite.Color;
            sprite.Color = _overlaySprite.Color = GravityHelperAPI.Exports.GetColor((int)GravityType);

            base.Render();

            sprite.Color = oldSpriteColor;
            _overlaySprite.Color = oldOverlayColor;

            GravityHelperAPI.Exports.EndCustomTintShader();
        }
        else
            base.Render();
    }
}
