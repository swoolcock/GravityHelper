// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[Tracked]
public class GravityRefillIndicator : Entity
{
    private const int columns = 3;

    private string currentKey => GravityHelperModule.ShouldInvertPlayer ? "down" : "up";

    private readonly Sprite _sprite;

    public GravityRefillIndicator()
    {
        Depth = Depths.Top;

        Tag = Tags.Persistent;

        Add(_sprite = GFX.SpriteBank.Create("gravityRefillIndicator"));
    }

    public override void Update()
    {
        base.Update();

        if (Scene.Tracker.GetEntity<Player>() is not { } player)
        {
            Visible = false;
            return;
        }

        var charges = GravityHelperModule.PlayerComponent?.GravityCharges ?? 0;
        var hasCharges = charges > 0;
        var offset = new Vector2(0, GravityHelperModule.ShouldInvertPlayer ? 20f : -20f);
        Position = player.Position + offset;
        Visible = hasCharges;

        var key = currentKey;

        if (_sprite.Animating && !hasCharges)
            _sprite.Stop();
        else if (!_sprite.Animating && hasCharges)
            _sprite.Play(key, true);
        else if (_sprite.Animating && _sprite.CurrentAnimationID != key)
        {
            var frame = _sprite.CurrentAnimationFrame;
            _sprite.Play(key);
            _sprite.SetAnimationFrame(frame);
        }
    }

    public override void Render()
    {
        if (!_sprite.Animating) return;

        var numberOfCharges = GravityHelperModule.PlayerComponent?.GravityCharges ?? 0;
        if (numberOfCharges == 0) return;

        var rows = (int)Math.Ceiling(numberOfCharges / (float)columns);
        var finalRowItems = numberOfCharges % columns;
        if (finalRowItems == 0) finalRowItems = columns;
        var yOffset = (int)_sprite.Height;
        var xHalfOffset = (int)Math.Ceiling(_sprite.Width / 2f + 1);

        if (!GravityHelperModule.ShouldInvertPlayer)
            yOffset = -yOffset;

        using var _ = GravityHelperAPI.Exports.WithCustomTintShader();
        _sprite.Color =
            GravityHelperModule.Settings.ColorSchemeType == GravityHelperModuleSettings.ColorSchemeSetting.Default
                ? Color.White
                : GravityType.Toggle.Color();

        for (int row = 0; row < rows; row++)
        {
            _sprite.Position.Y = row * yOffset;
            int x = (columns - 1) * -xHalfOffset;
            int rowItems = row < rows - 1 ? columns : finalRowItems;
            x += (columns - rowItems) * xHalfOffset;
            for (int column = 0; column < rowItems; column++)
            {
                _sprite.Position.X = x;
                x += 2 * xHalfOffset;
                _sprite.Render();
            }
        }
    }
}
