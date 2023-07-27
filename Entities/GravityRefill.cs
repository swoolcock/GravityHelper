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
    [CustomEntity("GravityHelper/GravityRefill")]
    public class GravityRefill : Entity
    {
        // properties
        public bool OneUse { get; }
        public int Charges { get; }
        public int Dashes { get; }
        public bool RefillsDash { get; }
        public bool RefillsStamina { get; }
        public float RespawnTime { get; }

        private readonly VersionInfo _modVersion;
        private readonly VersionInfo _pluginVersion;

        // components
        private readonly Sprite _sprite;
        private readonly Sprite _arrows;
        private readonly Image _outline;
        private readonly Wiggler _wiggler;
        private readonly BloomPoint _bloom;
        private readonly VertexLight _light;
        private readonly SineWave _sine;

        // particles
        public static readonly ParticleType P_Shatter = new ParticleType(Refill.P_Shatter)
        {
            Color = Color.Purple,
            Color2 = Color.MediumPurple,
        };

        public static readonly ParticleType P_Regen = new ParticleType(Refill.P_Regen)
        {
            Color = Color.BlueViolet,
            Color2 = Color.Violet,
        };

        public static readonly ParticleType P_Glow_Normal = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Blue,
            Color2 = Color.BlueViolet,
        };

        public static readonly ParticleType P_Glow_Inverted = new ParticleType(Refill.P_Glow)
        {
            Color = Color.Red,
            Color2 = Color.MediumVioletRed,
        };

        private Level _level;
        private float _respawnTimeRemaining;
        private float _arrowIntervalOffset;

        private bool _emitNormal;

        private const string gravity_toggle_charges = "GravityHelper_toggle_charges";
        public static int NumberOfCharges
        {
            get => (Engine.Scene as Level)?.Session.GetCounter(gravity_toggle_charges) ?? 0;
            set => (Engine.Scene as Level)?.Session.SetCounter(gravity_toggle_charges, value);
        }

        public GravityRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            Charges = data.Int("charges", 1);
            Dashes = data.Int("dashes", -1);
            OneUse = data.Bool("oneUse");
            RefillsDash = data.Bool("refillsDash", true);
            RefillsStamina = data.Bool("refillsStamina", true);
            RespawnTime = data.Float("respawnTime", 2.5f);

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Depth = Depths.Pickups;

            var path = "objects/GravityHelper/gravityRefill";
            var outlineName = Dashes == 2 ? "outline_two_dash" : "outline";
            var animationName = !RefillsDash ? "idle_no_dash" : Dashes == 2 ? "idle_two_dash" : "idle";

            // add components
            Add(new PlayerCollider(OnPlayer),
                _outline = new Image(GFX.Game[$"{path}/{outlineName}"]) {Visible = false},
                _sprite = GFX.SpriteBank.Create("gravityRefill"),
                _arrows = GFX.SpriteBank.Create("gravityRefillArrows"),
                _wiggler = Wiggler.Create(1f, 4f, v => _sprite.Scale = Vector2.One * (float) (1.0 + (double) v * 0.2)),
                new MirrorReflection(),
                _bloom = new BloomPoint(0.8f, 16f),
                _light = new VertexLight(Color.White, 1f, 16, 48),
                _sine = new SineWave(0.6f, 0.0f));

            _outline.CenterOrigin();

            // this uses a random sample but so as to not break existing maps i'm leaving it above the PushRandomDisposable
            _sprite.Play(animationName, true, true);

            using var _ = new PushRandomDisposable(data.ID);
            _sine.Randomize();
            _arrows.OnFinish = _ => _arrows.Visible = false;
            _arrowIntervalOffset = Calc.Random.NextFloat(2f);

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

            if (_respawnTimeRemaining > 0.0)
            {
                _respawnTimeRemaining -= Engine.DeltaTime;
                if (_respawnTimeRemaining <= 0.0)
                    respawn();
            }
            else if (Scene.OnInterval(0.1f))
            {
                var offset = Vector2.UnitY * (_emitNormal ? 5f : -5f);
                var range = Vector2.One * 4f;
                var direction = Vector2.UnitY.Angle() * (_emitNormal ? 1 : -1);
                var p_glow = _emitNormal ? P_Glow_Normal : P_Glow_Inverted;
                _level.ParticlesFG.Emit(p_glow, 1, Position + offset, range, direction);
                _emitNormal = !_emitNormal;
            }

            updateY();

            _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0.0f, 4f * Engine.DeltaTime);
            _bloom.Alpha = _light.Alpha * 0.8f;

            if (!Scene.OnInterval(2f, _arrowIntervalOffset) || !_sprite.Visible) return;

            var arrowName = Dashes == 2 ? "arrows_two_dash" : "arrows";
            _arrows.Play(arrowName, true);
            _arrows.Visible = true;
        }

        private void respawn()
        {
            if (Collidable) return;
            Collidable = true;

            _sprite.Visible = true;
            _outline.Visible = false;
            _arrows.Stop();
            Depth = Depths.Pickups;
            _wiggler.Start();
            Audio.Play("event:/game/general/diamond_return", Position);
            _level.ParticlesFG.Emit(P_Regen, 16, Position, Vector2.One * 2f);
        }

        private void updateY() => _arrows.Y = _sprite.Y = _bloom.Y = _sine.Value * 2f;

        public override void Render()
        {
            if (_sprite.Visible)
                _sprite.DrawOutline();
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            var dashes = Dashes < 0 ? player.MaxDashes : Dashes;
            bool canUse = RefillsDash && player.Dashes < dashes ||
                          RefillsStamina && player.Stamina < 20 ||
                          NumberOfCharges < Charges;

            if (!canUse) return;

            if (RefillsDash && Dashes < 0)
                player.RefillDash();
            else if (RefillsDash && player.Dashes < Dashes)
                player.Dashes = Dashes;

            if (RefillsStamina) player.RefillStamina();
            NumberOfCharges = Charges;

            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(refillRoutine(player)));
            _respawnTimeRemaining = RespawnTime;
        }

        private IEnumerator refillRoutine(Player player)
        {
            GravityRefill refill = this;
            Celeste.Freeze(0.05f);
            yield return null;

            refill._level.Shake();
            refill._sprite.Visible = refill._arrows.Visible = false;
            if (!refill.OneUse)
                refill._outline.Visible = true;
            refill.Depth = Depths.BGDecals - 1;
            yield return 0.05f;

            float direction = player.Speed.Angle();
            refill._level.ParticlesFG.Emit(P_Shatter, 5, refill.Position, Vector2.One * 4f, direction - (float)Math.PI / 2f);
            refill._level.ParticlesFG.Emit(P_Shatter, 5, refill.Position, Vector2.One * 4f, direction + (float)Math.PI / 2f);
            SlashFx.Burst(refill.Position, direction);

            if (refill.OneUse)
                refill.RemoveSelf();
        }
    }
}
