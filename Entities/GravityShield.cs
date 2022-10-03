// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityShield")]
    public class GravityShield : Entity
    {
        public bool OneUse { get; }
        public float RespawnTime { get; }
        public float ShieldTime { get; }

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private Level _level;
        private float _respawnTimeRemaining;
        private bool _emitNormal;

        private readonly Image _outline;
        private readonly Sprite _sprite;
        private readonly Wiggler _wiggler;
        private readonly BloomPoint _bloom;
        private readonly VertexLight _light;
        private readonly SineWave _sine;

        private readonly ParticleType p_shatter = new ParticleType(Refill.P_Shatter)
        {
            Color = Color.Purple,
            Color2 = Color.MediumPurple,
        };

        private readonly ParticleType p_regen = new ParticleType(Refill.P_Regen)
        {
            Color = Color.BlueViolet,
            Color2 = Color.Violet,
        };

        private readonly ParticleType p_glow_normal = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Blue,
            Color2 = Color.BlueViolet,
        };

        private readonly ParticleType p_glow_inverted = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Red,
            Color2 = Color.MediumVioletRed,
        };

        public GravityShield(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            OneUse = data.Bool("oneUse");
            RespawnTime = data.Float("respawnTime", 2.5f);
            ShieldTime = data.Float("shieldTime", 3f);

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Depth = Depths.Pickups;

            var path = "objects/GravityHelper/gravityShield";

            Add(new PlayerCollider(onPlayer),
                _outline = new Image(GFX.Game[$"{path}/outline"]) {Visible = false},
                _sprite = GFX.SpriteBank.Create("gravityShield"),
                _wiggler = Wiggler.Create(1f, 4f, v => _sprite.Scale = Vector2.One * (float) (1.0 + (double) v * 0.2)),
                new MirrorReflection(),
                _bloom = new BloomPoint(0.8f, 16f),
                _light = new VertexLight(Color.White, 1f, 16, 48),
                _sine = new SineWave(0.6f, 0.0f));

            _outline.CenterOrigin();
            _sprite.Play("idle", true, true);
            _sine.Randomize();

            updateY();
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            _level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();

            if (_respawnTimeRemaining > 0)
            {
                _respawnTimeRemaining -= Engine.DeltaTime;
                if (_respawnTimeRemaining <= 0)
                    respawn();
            }
            else if (Scene.OnInterval(0.1f))
            {
                var offset = Vector2.UnitX * (_emitNormal ? 5f : -5f);
                var range = Vector2.One * 4f;
                var direction = _emitNormal ? Vector2.UnitX.Angle() : -Vector2.UnitX.Angle();
                var p_glow = _emitNormal ? p_glow_normal : p_glow_inverted;
                _level.ParticlesFG.Emit(p_glow, 1, Position + offset, range, direction);
                _emitNormal = !_emitNormal;
            }

            updateY();

            _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0.0f, 4f * Engine.DeltaTime);
            _bloom.Alpha = _light.Alpha * 0.8f;
        }

        public override void Render()
        {
            if (_sprite.Visible)
                _sprite.DrawOutline();
            base.Render();
        }

        private void updateY() => _sprite.Y = _bloom.Y = _sine.Value * 2f;

        private void respawn()
        {
            if (Collidable) return;
            Collidable = true;

            _sprite.Visible = true;
            _outline.Visible = false;
            Depth = Depths.Pickups;
            _wiggler.Start();
            Audio.Play("event:/game/general/diamond_return", Position);
            _level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
        }

        private void onPlayer(Player player)
        {
            if (Scene?.Tracker.GetEntity<GravityShieldIndicator>() is not { } indicator) return;

            if (ShieldTime > indicator.ShieldTimeRemaining)
            {
                indicator.Activate(ShieldTime);
                Audio.Play("event:/game/general/diamond_touch", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine(shieldRoutine(player)));
                _respawnTimeRemaining = RespawnTime;
            }
        }

        private IEnumerator shieldRoutine(Player player)
        {
            GravityShield shield = this;
            Celeste.Freeze(0.05f);
            yield return null;

            shield._level.Shake();
            shield._sprite.Visible = false;
            if (!shield.OneUse)
                shield._outline.Visible = true;
            shield.Depth = Depths.BGDecals - 1;
            yield return 0.05f;

            float direction = player.Speed.Angle();
            shield._level.ParticlesFG.Emit(shield.p_shatter, 5, shield.Position, Vector2.One * 4f, direction - (float)Math.PI / 2f);
            shield._level.ParticlesFG.Emit(shield.p_shatter, 5, shield.Position, Vector2.One * 4f, direction + (float)Math.PI / 2f);
            SlashFx.Burst(shield.Position, direction);

            if (shield.OneUse)
                shield.RemoveSelf();
        }
    }
}
