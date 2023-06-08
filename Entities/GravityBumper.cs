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
    [CustomEntity("GravityHelper/GravityBumper")]
    public class GravityBumper : Bumper
    {
        // ReSharper disable InconsistentNaming
        private static ParticleType P_Ambience_Normal;
        private static ParticleType P_Ambience_Inverted;
        private static ParticleType P_Ambience_Toggle;
        private static ParticleType P_Launch_Normal;
        private static ParticleType P_Launch_Inverted;
        private static ParticleType P_Launch_Toggle;
        // ReSharper restore InconsistentNaming

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        public GravityType GravityType { get; }

        private readonly DynamicData _data;
        private readonly Sprite _sprite;
        private readonly Sprite _rippleSprite;
        private readonly Sprite _maskSprite;

        public GravityBumper(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _data = DynamicData.For(this);

            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");

            var sine = _data.Get<SineWave>("sine");
            sine.Rate = data.Float("wobbleRate", 1f);

            _sprite = _data.Get<Sprite>("sprite");

            _maskSprite = GFX.SpriteBank.Create("gravityBumper");
            _maskSprite.Play("mask");

            Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
            _rippleSprite.Color = GravityType.Color();
            _rippleSprite.Play("loop");
        }

        public ParticleType GetAmbientParticleType()
        {
            if (P_Ambience_Normal == null)
            {
                const float lightness = 0.5f;
                P_Ambience_Normal = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Normal.Color(),
                    Color2 = GravityType.Normal.Color().Lighter(lightness),
                };
                P_Ambience_Inverted = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Inverted.Color(),
                    Color2 = GravityType.Inverted.Color().Lighter(lightness),
                };
                P_Ambience_Toggle = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Toggle.Color(),
                    Color2 = GravityType.Toggle.Color().Lighter(lightness),
                };
            }

            return GravityType switch
            {
                GravityType.Normal => P_Ambience_Normal,
                GravityType.Inverted => P_Ambience_Inverted,
                GravityType.Toggle => P_Ambience_Toggle,
                _ => P_Ambience,
            };
        }

        public ParticleType GetLaunchParticleType()
        {
            if (P_Launch_Normal == null)
            {
                const float lightness = 0.5f;
                P_Launch_Normal = new ParticleType(P_Launch)
                {
                    Color = GravityType.Normal.Color(),
                    Color2 = GravityType.Normal.Color().Lighter(lightness),
                };
                P_Launch_Inverted = new ParticleType(P_Launch)
                {
                    Color = GravityType.Inverted.Color(),
                    Color2 = GravityType.Inverted.Color().Lighter(lightness),
                };
                P_Launch_Toggle = new ParticleType(P_Launch)
                {
                    Color = GravityType.Toggle.Color(),
                    Color2 = GravityType.Toggle.Color().Lighter(lightness),
                };
            }

            return GravityType switch
            {
                GravityType.Normal => P_Launch_Normal,
                GravityType.Inverted => P_Launch_Inverted,
                GravityType.Toggle => P_Launch_Toggle,
                _ => P_Launch,
            };
        }

        public override void Render()
        {
            if (_sprite.Visible)
            {
                var animation = _maskSprite.Animations["mask"];
                var frameIndex = 0;
                if (_sprite.CurrentAnimationID == "hit")
                    frameIndex = _sprite.CurrentAnimationFrame >= 2 ? _sprite.CurrentAnimationFrame - 1 : 0;
                else if (_sprite.CurrentAnimationID == "off")
                    frameIndex = 7;
                else if (_sprite.CurrentAnimationID == "on")
                    frameIndex = (_sprite.CurrentAnimationFrame + 7) % 9;
                var frame = animation.Frames.ElementAtOrDefault(frameIndex);

                if (frame != null)
                {
                    var color = GravityType.Color();
                    if (frameIndex == 7)
                        color *= 0.5f;

                    frame.DrawCentered(Position + _sprite.Position, color);
                }
            }

            base.Render();
        }

        public override void Update()
        {
            base.Update();

            const float ripple_offset = 8f;
            var currentGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;

            if (GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && currentGravity == GravityType.Normal)
            {
                _rippleSprite.Y = -ripple_offset;
                _rippleSprite.Scale.Y = 1f;
            }
            else if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted)
            {
                _rippleSprite.Y = ripple_offset;
                _rippleSprite.Scale.Y = -1f;
            }
        }
    }
}
