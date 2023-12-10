// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBadelineBoost")]
    public class GravityBadelineBoost : BadelineBoost
    {
        // ReSharper disable NotAccessedField.Local
        private readonly VersionInfo _modVersion;
        private readonly VersionInfo _pluginVersion;
        // ReSharper restore NotAccessedField.Local

        public GravityType GravityType { get; }
        public string NodeGravityTypes { get; }

        private readonly Sprite _rippleSprite;
        private readonly Sprite _maskSprite;
        private readonly GravityType[] _gravityTypes;

        public GravityType CurrentDirection { get; private set; } = GravityType.Normal;

        private GravityType currentNodeGravityType => _gravityTypes == null ? GravityType : _gravityTypes[nodeIndex];

        public GravityBadelineBoost(EntityData data, Vector2 offset)
            : base(data.NodesWithPosition(offset), data.Bool("lockCamera", true), data.Bool("canSkip"))
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

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

            if (sprite.Visible && !travelling && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                _rippleSprite.Visible = true;
                _rippleSprite.Position = sprite.Position;

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
            if (sprite.Visible)
            {
                var animation = _maskSprite.Animations["mask"];
                var frameIndex = 0;
                if (sprite.CurrentAnimationID == "blink" && sprite.CurrentAnimationFrame < 3)
                    frameIndex = sprite.CurrentAnimationFrame + 1;
                var frame = animation.Frames[frameIndex];

                frame.DrawCentered(Position + sprite.Position, currentNodeGravityType.Color());
            }

            base.Render();
        }
    }
}
