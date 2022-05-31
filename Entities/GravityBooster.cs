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

        private readonly Sprite _animationSprite;

        public GravityBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Bool("red"))
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");

            Add(_animationSprite = GFX.SpriteBank.Create("gravityRipple"));
            _animationSprite.Color = GravityType.Color();
            _animationSprite.Play("loop");
        }

        public override void Update()
        {
            base.Update();

            const float ripple_offset = 5f;
            var currentGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;

            if (GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && currentGravity == GravityType.Normal)
            {
                _animationSprite.Y = -ripple_offset;
                _animationSprite.Scale.Y = 1f;
            }
            else if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted)
            {
                _animationSprite.Y = ripple_offset;
                _animationSprite.Scale.Y = -1f;
            }
        }
    }
}
