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

        private readonly DynData<BadelineBoost> _data;
        private readonly Sprite _sprite;
        private readonly Sprite _animationSprite;
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

            _data = new DynData<BadelineBoost>(this);
            _sprite = _data.Get<Sprite>("sprite");

            GravityType = data.Enum<GravityType>("gravityType");
            NodeGravityTypes = data.Attr("nodeGravityTypes", string.Empty);

            var nodeTypes = NodeGravityTypes.Split(',');
            if (nodeTypes.Length == data.Nodes.Length + 1)
                _gravityTypes = nodeTypes.Select(s => int.TryParse(s, out var value) ? (GravityType)value : GravityType.None).ToArray();
            else
                _gravityTypes = null;

            Add(_animationSprite = GFX.SpriteBank.Create("gravityBadelineBoost"));
            _animationSprite.Play("ripple");
            _animationSprite.Color = GravityType.Color();

            _maskSprite = GFX.SpriteBank.Create("gravityBadelineBoost");
            _maskSprite.Play("mask");
        }

        public override void Update()
        {
            base.Update();

            _animationSprite.Visible = false;

            if (_sprite.Visible && !travelling && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                _animationSprite.Visible = true;
                _animationSprite.Position = _sprite.Position;

                var currentPlayerGravity = playerComponent.CurrentGravity;
                var currentNodeGravity = currentNodeGravityType;

                if (_gravityTypes != null)
                    _animationSprite.Color = currentNodeGravity.Color();

                if (currentNodeGravity == GravityType.Normal ||
                    currentNodeGravity == GravityType.Toggle && currentPlayerGravity == GravityType.Inverted ||
                    currentNodeGravity == GravityType.None && currentPlayerGravity == GravityType.Normal)
                {
                    _animationSprite.Rotation = 0;
                    CurrentDirection = GravityType.Normal;
                }
                else if (currentNodeGravity == GravityType.Inverted ||
                    currentNodeGravity == GravityType.Toggle && currentPlayerGravity == GravityType.Normal ||
                    currentNodeGravity == GravityType.None && currentPlayerGravity == GravityType.Inverted)
                {
                    _animationSprite.Rotation = (float)Math.PI;
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

                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    if (x != 0 || y != 0)
                        frame.DrawCentered(Position + _sprite.Position + new Vector2(x, y), currentNodeGravityType.Color());
                }
            }

            base.Render();
        }
    }
}
