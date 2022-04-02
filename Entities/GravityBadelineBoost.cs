// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBadelineBoost")]
    public class GravityBadelineBoost : BadelineBoost
    {
        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        public GravityType GravityType { get; }

        private readonly DynData<BadelineBoost> _data;
        private readonly Sprite _sprite;
        private readonly Sprite _animationSprite;

        public GravityType CurrentDirection { get; private set; } = GravityType.Normal;

        private bool travelling => _data.Get<bool>("travelling");

        public GravityBadelineBoost(EntityData data, Vector2 offset)
            : base(data.NodesWithPosition(offset), data.Bool("lockCamera", true), data.Bool("canSkip"))
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            _data = new DynData<BadelineBoost>(this);
            _sprite = _data.Get<Sprite>("sprite");

            GravityType = data.Enum<GravityType>("gravityType");

            Add(_animationSprite = GFX.SpriteBank.Create("gravityBadelineBoost"));
            _animationSprite.Play("ripple");
            _animationSprite.Color = GravityType.Color();
        }

        public override void Update()
        {
            base.Update();

            _animationSprite.Visible = false;

            if (_sprite.Visible && !travelling && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                _animationSprite.Visible = true;
                _animationSprite.Position = _sprite.Position;

                var currentGravity = playerComponent.CurrentGravity;

                if (GravityType == GravityType.Normal ||
                    GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted ||
                    GravityType == GravityType.None && currentGravity == GravityType.Normal)
                {
                    _animationSprite.Rotation = 0;
                    CurrentDirection = GravityType.Normal;
                }
                else if (GravityType == GravityType.Inverted ||
                    GravityType == GravityType.Toggle && currentGravity == GravityType.Normal ||
                    GravityType == GravityType.None && currentGravity == GravityType.Inverted)
                {
                    _animationSprite.Rotation = (float)Math.PI;
                    CurrentDirection = GravityType.Inverted;
                }
            }
        }
    }
}
