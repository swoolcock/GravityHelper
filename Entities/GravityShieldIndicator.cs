// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [Tracked]
    public class GravityShieldIndicator : Entity
    {
        public float ShieldTotalTime { get; set; }
        public float ShieldTimeRemaining { get; set; }

        private const float flash_time = 1f;
        private bool _flash;

        private const int particle_count = 20;
        private const float radius = 14f;
        private readonly List<Vector2> _particles = new List<Vector2>();
        private readonly float[] _speeds = {1.5f, 2f, 2.5f};

        private readonly Color[] _particleColors = {
            Color.Blue,
            Color.BlueViolet,
            Color.Red,
            Color.MediumVioletRed,
        };

        private readonly BloomPoint _bloom;
        private readonly VertexLight _light;

        private const float bloom_alpha = 0.8f;
        private const float light_alpha = 1f;

        public GravityShieldIndicator()
        {
            Depth = Depths.Top;
            Tag = Tags.Persistent;
            Visible = false;
            Active = false;

            Add(_bloom = new BloomPoint(0f, 16f),
                _light = new VertexLight(Color.White, 0f, 16, 48));

            for (int index = 0; index < particle_count; ++index)
            {
                float angle = Calc.Random.NextAngle();
                float length = (radius - 1) * (float)Math.Sqrt(Calc.Random.NextFloat());
                _particles.Add(new Vector2((float)Math.Cos(angle) * length, (float)Math.Sin(angle) * length));
            }
        }

        public override void Update()
        {
            base.Update();

            if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
            var offset = GravityHelperModule.ShouldInvertPlayer ? 4f : -4f;
            Position = player.Center + Vector2.UnitY * offset;

            if (ShieldTimeRemaining > 0)
            {
                for (int i = 0; i < _particles.Count; i++)
                {
                    float maxMove = radius * _speeds[i % _speeds.Length];
                    _particles[i] = Calc.Approach(_particles[i], Vector2.Zero, maxMove * Engine.DeltaTime);
                    if (_particles[i].LengthSquared() < 8 * 8)
                    {
                        float angle = Calc.Random.NextAngle();
                        const float length = radius - 1;
                        _particles[i] = new Vector2(length * (float)Math.Cos(angle), length * (float)Math.Sin(angle));
                    }
                }
                ShieldTimeRemaining -= Engine.DeltaTime;
                if (ShieldTimeRemaining <= 0)
                    Deactivate();
            }

            if (ShieldTimeRemaining <= flash_time)
            {
                if (Scene.OnInterval(0.1f)) _flash = !_flash;
                float amount = Ease.QuintOut(ShieldTimeRemaining / flash_time);
                _light.Alpha = light_alpha * amount;
                _bloom.Alpha = bloom_alpha * amount;
            }
        }


        public override void Render()
        {
            if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;

            if (!_flash)
            {
                var offset = GravityHelperModule.ShouldInvertPlayer ? 4f : -4f;
                var origin = player.Center + Vector2.UnitY * offset;

                for (var i = 0; i < _particles.Count; i++)
                {
                    var part = _particles[i];
                    var color = _particleColors[i % _particleColors.Length];
                    Draw.Pixel.Draw(origin + part, Vector2.Zero, color * 0.7f);
                }

                Draw.Circle(origin, radius, Color.Violet * 0.7f, 3);
            }
        }

        public void Activate(float time)
        {
            if (GravityHelperModule.PlayerComponent is not { } playerComponent) return;
            ShieldTimeRemaining = ShieldTotalTime = time;
            Active = Visible = true;
            playerComponent.Locked = true;
            _flash = false;

            _bloom.Alpha = bloom_alpha;
            _light.Alpha = light_alpha;
        }

        public void Deactivate()
        {
            ShieldTimeRemaining = ShieldTotalTime = 0;
            Active = Visible = false;
            if (GravityHelperModule.PlayerComponent is { } playerComponent)
                playerComponent.Locked = false;

            _bloom.Alpha = 0f;
            _light.Alpha = 0f;
        }
    }
}
