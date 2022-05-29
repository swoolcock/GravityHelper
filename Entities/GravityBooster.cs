// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBooster")]
    public class GravityBooster : Booster
    {
        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        public GravityType GravityType { get; }

        private readonly Sprite _animationSpriteUp;
        private readonly Sprite _animationSpriteDown;

        public GravityBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Bool("red"))
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");

            const float ripple_offset = 5f;

            if (GravityType == GravityType.Inverted || GravityType == GravityType.Toggle)
            {
                Add(_animationSpriteUp = GFX.SpriteBank.Create("gravityBooster"));
                _animationSpriteUp.Y -= ripple_offset;
                _animationSpriteUp.Color = GravityType.Color();
                _animationSpriteUp.Play("ripple");
            }

            if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle)
            {
                Add(_animationSpriteDown = GFX.SpriteBank.Create("gravityBooster"));
                _animationSpriteDown.Scale.Y = -1f;
                _animationSpriteDown.Y += ripple_offset;
                _animationSpriteDown.Color = GravityType.Color();
                _animationSpriteDown.Play("ripple");
            }
        }
    }
}
