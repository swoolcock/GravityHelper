// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityIndicator")]
    public class GravityIndicator : Entity
    {
        public bool ShowRipples { get; }
        public bool ShowParticles { get; }
        public float BloomAlpha { get; }
        public float BloomRadius { get; }
        public float IdleAlpha { get; }
        public float TurningAlpha { get; }
        public float TurnTime { get; }

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly Sprite _arrowSprite;
        private readonly Sprite _rippleSprite;
        private readonly VertexLight _vertexLight;

        private readonly Color _normalLightColor = new Color(0.3f, 0.3f, 1f);
        private readonly Color _invertedLightColor = new Color(1f, 0.3f, 0.3f);

        private const int up_arrow_frame = 0;
        private const int down_arrow_frame = 8;

        private readonly ParticleType p_glow_normal = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Blue,
            Color2 = Color.BlueViolet,
            DirectionRange = (float)(Math.PI / 2),
        };

        private readonly ParticleType p_glow_inverted = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Red,
            Color2 = Color.MediumVioletRed,
            DirectionRange = (float)(Math.PI / 2),
        };

        public GravityIndicator(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            ShowRipples = data.Bool("showRipples", false);
            ShowParticles = data.Bool("showParticles", true);
            Depth = data.Int("depth", 8500);
            BloomAlpha = data.Float("bloomAlpha", 0.6f);
            BloomRadius = data.Float("bloomRadius", 14f);
            IdleAlpha = data.Float("idleAlpha", 1f).Clamp(0f, 1f);
            TurningAlpha = data.Float("turningAlpha", 0.4f).Clamp(0f, 1f);
            TurnTime = data.Float("turnTime", 0.3f).ClampLower(0.1f);

            Add(_arrowSprite = GFX.SpriteBank.Create("gravityIndicator"));
            _arrowSprite.Rate = 0f;
            _arrowSprite.Play("arrow");
            _arrowSprite.Color = Color.White * IdleAlpha;

            var arrowAnimation = _arrowSprite.Animations["arrow"];
            arrowAnimation.Delay = TurnTime / (arrowAnimation.Frames.Length / 2f);

            if (ShowRipples)
                Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));

            if (BloomAlpha > 0 && BloomRadius > 0)
            {
                Add(new BloomPoint(BloomAlpha, BloomRadius));
                Add(_vertexLight = new VertexLight(Color.Red, 1f, (int)BloomRadius, (int)BloomRadius));
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            _arrowSprite.SetAnimationFrame(GravityHelperModule.ShouldInvertPlayer ? up_arrow_frame : down_arrow_frame);
            updateRipple();
        }

        public override void Update()
        {
            updateArrow();
            updateRipple();
            updateParticles();

            base.Update();
        }

        private void updateArrow()
        {
            var shouldInvert = GravityHelperModule.ShouldInvertPlayer;
            if (shouldInvert && _arrowSprite.CurrentAnimationFrame == up_arrow_frame ||
                !shouldInvert && _arrowSprite.CurrentAnimationFrame == down_arrow_frame)
            {
                _arrowSprite.Rate = 0f;
                _arrowSprite.Color = Color.White * IdleAlpha;
            }
            else if (shouldInvert && _arrowSprite.CurrentAnimationFrame < down_arrow_frame ||
                !shouldInvert && _arrowSprite.CurrentAnimationFrame > down_arrow_frame)
            {
                _arrowSprite.Rate = -1f;
                _arrowSprite.Color = Color.White * TurningAlpha;
            }
            else
            {
                _arrowSprite.Rate = 1f;
                _arrowSprite.Color = Color.White * TurningAlpha;
            }
        }

        private void updateRipple()
        {
            if (_rippleSprite != null)
            {
                var upArrowFrame = _arrowSprite.CurrentAnimationFrame == up_arrow_frame;
                var downArrowFrame = _arrowSprite.CurrentAnimationFrame == down_arrow_frame;
                float multiplier = upArrowFrame ? 1 : -1;
                const float ripple_offset = 2f;
                _rippleSprite.Y = ripple_offset * -multiplier;
                _rippleSprite.Scale.Y = multiplier;
                _rippleSprite.Color = (upArrowFrame ? GravityType.Inverted : GravityType.Normal).Color() * 0.8f;
                _rippleSprite.Visible = upArrowFrame || downArrowFrame;
            }
        }

        private void updateParticles()
        {
            if (!ShowParticles) return;

            if (_arrowSprite.CurrentAnimationFrame == up_arrow_frame || _arrowSprite.CurrentAnimationFrame == down_arrow_frame)
            {
                var emitNormal = _arrowSprite.CurrentAnimationFrame == down_arrow_frame;
                _vertexLight.Color = (emitNormal ? _normalLightColor : _invertedLightColor) * IdleAlpha;

                if (Scene is Level level && level.OnInterval(0.1f))
                {
                    var offset = Vector2.UnitY * (emitNormal ? 5f : -5f);
                    var range = Vector2.One * 6f;
                    var direction = Vector2.UnitY.Angle() * (emitNormal ? 1 : -1);
                    var particleType = emitNormal ? p_glow_normal : p_glow_inverted;
                    level.ParticlesBG.Emit(particleType, 2, Position + offset, range, direction);
                }
            }
            else
            {
                _vertexLight.Color = Color.White * TurningAlpha;
            }
        }
    }
}
