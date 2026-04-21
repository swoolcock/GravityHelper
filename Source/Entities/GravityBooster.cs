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

    private readonly string _textureDirectory;
    private readonly bool _showOverlay;
    private readonly bool _showRipple;

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

        _textureDirectory = data.Attr("textureDirectory").Trim();
        _showOverlay = data.Bool("showOverlay", true);
        _showRipple = data.Bool("showRipple", true);

        GravityType = (GravityType)data.Int("gravityType");

        var spriteName = red ? "gravityBoosterRed" : "gravityBooster";
        if (string.IsNullOrWhiteSpace(_textureDirectory))
            GFX.SpriteBank.CreateOn(sprite, spriteName);
        else
            GFX.SpriteBank.CreateOnWithPath(sprite, spriteName, _textureDirectory);

        if (_showOverlay)
        {
            Add(_overlaySprite = sprite.CreateClone());
            _overlaySprite.Play(overlayId);
        }

        if (_showRipple)
        {
            Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
            _rippleSprite.Play("loop");
        }

        Add(new AccessibilityListener(onAccessibilityChange));
        onAccessibilityChange();
    }

    private void onAccessibilityChange()
    {
        _rippleSprite?.Color = GravityType.RippleColor();
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
            _rippleSprite?.Y = -ripple_offset;
            _rippleSprite?.Scale.Y = 1f;
            // change to the correct loop if we need to
            if (sprite.CurrentAnimationID == "loop_down")
                sprite.Play("loop");
        }
        else if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted)
        {
            _rippleSprite?.Y = ripple_offset;
            _rippleSprite?.Scale.Y = -1f;
            // change to the correct loop if we need to
            if (sprite.CurrentAnimationID == "loop")
                sprite.Play("loop_down");
        }

        if (GravityType == GravityType.Toggle)
        {
            _overlaySprite?.Scale.Y = currentGravity == GravityType.Normal ? -1 : 1;
        }

        var isVisible = sprite.CurrentAnimationID.StartsWith("loop");
        _rippleSprite?.Visible = isVisible;
        _overlaySprite?.Visible = isVisible;
    }

    public override void Render()
    {
        var scheme = GravityHelperModule.Settings.GetColorScheme();

        if (scheme.NeedsShader && _overlaySprite != null)
        {
            _overlaySprite.Visible = false;
            base.Render();
            _overlaySprite.Visible = true;

            using (GravityHelperAPI.InternalCustomTintShader())
            {
                _overlaySprite.Color = GravityType.Color(scheme).Saturation(2f);
                _overlaySprite.Render();
                _overlaySprite.Color = Color.White;
            }
        }
        else
            base.Render();
    }
}
