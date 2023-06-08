// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
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
        public string NodeGravityTypes { get; }

        private readonly DynamicData _data;
        private readonly Sprite _sprite;
        private readonly Sprite _rippleSprite;
        private readonly Sprite _maskSprite;
        private readonly GravityType[] _gravityTypes;

        public GravityType CurrentDirection { get; private set; } = GravityType.Normal;

        private bool travelling => _data.Get<bool>("travelling");
        private int nodeIndex => _data.Get<int>("nodeIndex");
        private GravityType currentNodeGravityType => _gravityTypes == null ? GravityType : _gravityTypes[nodeIndex];

        public GravityBadelineBoost(EntityData data, Vector2 offset)
            : base(data.NodesWithPosition(offset), data.Bool("lockCamera", true), data.Bool("canSkip"))
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            _data = DynamicData.For(this);
            _sprite = _data.Get<Sprite>("sprite");

            GravityType = data.Enum<GravityType>("gravityType");
            NodeGravityTypes = data.Attr("nodeGravityTypes", string.Empty);

            var nodeTypes = NodeGravityTypes.Split(',');
            if (nodeTypes.Length == data.Nodes.Length + 1)
                _gravityTypes = nodeTypes.Select(s => int.TryParse(s, out var value) ? (GravityType)value : GravityType.None).ToArray();
            else
                _gravityTypes = null;

            Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
            _rippleSprite.Play("loop");
            _rippleSprite.Color = GravityType.Color();

            _maskSprite = GFX.SpriteBank.Create("gravityBadelineBoost");
            _maskSprite.Play("mask");
        }

        public override void Update()
        {
            base.Update();

            _rippleSprite.Visible = false;

            if (_sprite.Visible && !travelling && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                _rippleSprite.Visible = true;
                _rippleSprite.Position = _sprite.Position;

                var currentPlayerGravity = playerComponent.CurrentGravity;
                var currentNodeGravity = currentNodeGravityType;

                if (_gravityTypes != null)
                    _rippleSprite.Color = currentNodeGravity.Color();

                if (currentNodeGravity == GravityType.Normal ||
                    currentNodeGravity == GravityType.Toggle && currentPlayerGravity == GravityType.Inverted ||
                    currentNodeGravity == GravityType.None && currentPlayerGravity == GravityType.Normal)
                {
                    _rippleSprite.Rotation = 0;
                    _rippleSprite.Position -= new Vector2(0f, 2f);
                    CurrentDirection = GravityType.Normal;
                }
                else if (currentNodeGravity == GravityType.Inverted ||
                    currentNodeGravity == GravityType.Toggle && currentPlayerGravity == GravityType.Normal ||
                    currentNodeGravity == GravityType.None && currentPlayerGravity == GravityType.Inverted)
                {
                    _rippleSprite.Rotation = (float)Math.PI;
                    _rippleSprite.Position += new Vector2(0f, 2f);
                    CurrentDirection = GravityType.Inverted;
                }
            }
        }

        public override void Render()
        {
            if (_sprite.Visible)
            {
                var animation = _maskSprite.Animations["mask"];
                var frameIndex = 0;
                if (_sprite.CurrentAnimationID == "blink" && _sprite.CurrentAnimationFrame < 3)
                    frameIndex = _sprite.CurrentAnimationFrame + 1;
                var frame = animation.Frames[frameIndex];

                frame.DrawCentered(Position + _sprite.Position, currentNodeGravityType.Color());
            }

            base.Render();
        }
    }
}
