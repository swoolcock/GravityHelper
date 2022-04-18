// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Triggers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityField")]
    [Tracked]
    public class GravityField : GravityTrigger, IConnectableField
    {
        public const float DEFAULT_ARROW_OPACITY = 0.5f;
        public const float DEFAULT_FIELD_OPACITY = 0.15f;
        public const float DEFAULT_PARTICLE_OPACITY = 0.5f;

        private const float audio_muffle_seconds = 0.2f;

        #region Entity Properties

        public VisualType ArrowType { get; }
        public VisualType FieldType { get; }
        public bool AttachToSolids { get; }

        #endregion

        // We'll always handle it ourselves to cover connected fields
        public override bool ShouldAffectPlayer => false;

        public Color FieldColor { get; private set; }

        private GravityType arrowGravityType => ArrowType == VisualType.Default ? GravityType : (GravityType) ArrowType;
        private GravityType fieldGravityType => FieldType == VisualType.Default ? GravityType : (GravityType) FieldType;

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly bool _shouldDrawArrows;
        private readonly bool _shouldDrawField;
        private readonly MTexture _arrowTexture;
        private readonly MTexture _arrowSmallTexture;
        private readonly Vector2 _arrowOrigin;
        private readonly Vector2 _arrowSmallOrigin;
        private Vector2 _arrowShakeOffset;

        private readonly Hitbox _normalHitbox;
        private readonly Hitbox _staticMoverHitbox;

        private readonly List<Vector2> _particles = new List<Vector2>();
        private readonly float[] _speeds = {12f, 20f, 40f};
        private GravityFieldGroup _fieldGroup;

        private float? _arrowOpacity;
        private float? _fieldOpacity;
        private float? _particleOpacity;
        private string _arrowColor;
        private string _fieldColor;
        private string _particleColor;
        private string _sound;

        private float _audioMuffleSecondsRemaining;

        public GravityField(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            AttachToSolids = data.Bool("attachToSolids");
            ArrowType = (VisualType)data.Int("arrowType", (int) VisualType.Default);
            FieldType = (VisualType)data.Int("fieldType", (int) VisualType.Default);

            _arrowOpacity = data.NullableFloat("arrowOpacity")?.Clamp(0f, 1f);
            _particleOpacity = data.NullableFloat("particleOpacity")?.Clamp(0f, 1f);
            _fieldOpacity = data.NullableFloat("fieldOpacity")?.Clamp(0f, 1f);
            _arrowColor = data.NullableAttr("arrowColor");
            _fieldColor = data.NullableAttr("fieldColor");
            _particleColor = data.NullableAttr("particleColor");
            _sound = data.NullableAttr("sound");

            _shouldDrawArrows = !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None);
            _shouldDrawField = !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);

            Visible = _shouldDrawArrows || _shouldDrawField;
            Collider = _normalHitbox = new Hitbox(data.Width, data.Height);

            if (_shouldDrawArrows && !_shouldDrawField)
                Depth = Depths.FGDecals - 1;
            else
                Depth = Depths.Player + 1;

            _staticMoverHitbox = new Hitbox(data.Width + 2, data.Height + 2, -1, -1);

            if (AttachToSolids)
            {
                Add(new StaticMover
                {
                    OnAttach = p => Depth = p.Depth - 1,
                    SolidChecker = staticMoverCollideCheck,
                    OnShake = amount => _arrowShakeOffset += amount,
                });
            }

            if (_shouldDrawArrows)
            {
                var prefix = arrowGravityType switch
                {
                    GravityType.Normal => "down",
                    GravityType.Inverted => "up",
                    _ => "double",
                };

                _arrowTexture = GFX.Game[$"objects/GravityHelper/gravityField/{prefix}Arrow"];
                _arrowSmallTexture = GFX.Game[$"objects/GravityHelper/gravityField/{prefix}ArrowSmall"];
                _arrowOrigin = new Vector2(_arrowTexture.Width / 2f, _arrowTexture.Height / 2f);
                _arrowSmallOrigin = new Vector2(_arrowSmallTexture.Width / 2f, _arrowSmallTexture.Height / 2f);
            }

            if (_shouldDrawField)
            {
                for (int index = 0; index < Width * (double) Height / 16.0; ++index)
                    _particles.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f)));
            }
        }

        protected override void HandleOnEnter(Player player)
        {
            if (GravityType == GravityType.None || !AffectsPlayer || _fieldGroup == null) return;

            _fieldGroup.Semaphore++;

            if (_fieldGroup.Semaphore == 1 && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                var previousGravity = playerComponent.CurrentGravity;
                playerComponent.SetGravity(GravityType, MomentumMultiplier);
                if (!string.IsNullOrWhiteSpace(_sound) && playerComponent.CurrentGravity != previousGravity && _audioMuffleSecondsRemaining <= 0)
                {
                    Audio.Play(_sound);
                    _audioMuffleSecondsRemaining = audio_muffle_seconds;
                }
            }
        }

        protected override void HandleOnStay(Player player)
        {
            if (GravityType == GravityType.None || !AffectsPlayer ||GravityType == GravityType.Toggle)
                return;

            if (GravityType != GravityHelperModule.PlayerComponent?.CurrentGravity)
                GravityHelperModule.PlayerComponent?.SetGravity(GravityType, MomentumMultiplier);
        }

        protected override void HandleOnLeave(Player player)
        {
            if (_fieldGroup == null) return;
            _fieldGroup.Semaphore--;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            var visual = Scene.GetActiveController<VisualGravityController>();
            _arrowOpacity ??= visual?.FieldArrowOpacity ?? DEFAULT_ARROW_OPACITY;
            _fieldOpacity ??= visual?.FieldBackgroundOpacity ?? DEFAULT_FIELD_OPACITY;
            _particleOpacity ??= visual?.FieldParticleOpacity ?? DEFAULT_PARTICLE_OPACITY;

            _fieldColor ??= GravityType switch
            {
                GravityType.Normal => visual?.FieldNormalColor,
                GravityType.Inverted => visual?.FieldInvertedColor,
                GravityType.Toggle => visual?.FieldToggleColor,
                _ => null,
            };

            FieldColor = (string.IsNullOrEmpty(_fieldColor) ? GravityType.Color() : Calc.HexToColor(_fieldColor)) * _fieldOpacity.Value;

            _arrowColor ??= visual?.FieldArrowColor;
            _particleColor ??= visual?.FieldParticleColor;

            if (_sound == null && Scene.GetActiveController<SoundGravityController>() is { } soundController)
            {
                if (GravityType == GravityType.Normal)
                    _sound = soundController.NormalSound;
                else if (GravityType == GravityType.Inverted)
                    _sound = soundController.InvertedSound;
                else if (GravityType == GravityType.Toggle)
                    _sound = soundController.ToggleSound;
            }

            if (_shouldDrawField)
                this.GetConnectedFieldRenderer<GravityFieldRenderer, GravityField>(scene, true);

            _arrowShakeOffset = Vector2.Zero;
            _fieldGroup = null;
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            if (_shouldDrawField)
                this.GetConnectedFieldRenderer<GravityFieldRenderer, GravityField>(scene, false);

            _fieldGroup = null;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            buildFieldGroup();
        }

        public override void Update()
        {
            if (_audioMuffleSecondsRemaining > 0)
                _audioMuffleSecondsRemaining -= Engine.DeltaTime;

            int length = _speeds.Length;
            float height = Height;
            int index = 0;

            for (int count = _particles.Count; index < count; ++index)
            {
                bool flip = GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && index % 2 == 1;
                Vector2 target = _particles[index] + Vector2.UnitY * _speeds[index % length] * Engine.DeltaTime * (flip ? -1 : 1);

                if (target.Y < 0)
                    target.Y += height;
                else if (target.Y >= height)
                    target.Y -= height;

                _particles[index] = target;
            }

            base.Update();
        }

        public override void Render()
        {
            base.Render();

            var particleOpacity = _particleOpacity ?? DEFAULT_PARTICLE_OPACITY;
            var arrowOpacity = _arrowOpacity ?? DEFAULT_ARROW_OPACITY;

            if (_shouldDrawField)
            {
                var color = (string.IsNullOrWhiteSpace(_particleColor) ? Color.White : Calc.HexToColor(_particleColor)) * particleOpacity;
                foreach (Vector2 particle in _particles)
                    Draw.Pixel.Draw(Position + particle, Vector2.Zero, color);
            }

            if (_shouldDrawArrows)
            {
                int widthInTiles = (int) (Width / 8);
                int heightInTiles = (int) (Height / 8);

                // one arrow every 2 tiles, rounded down, but at least one
                int arrowsX = Math.Max(widthInTiles / 2, 1);
                int arrowsY = Math.Max(heightInTiles / 2, 1);

                // if width or height is 1, scale down the arrows
                var texture = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallTexture : _arrowTexture;
                var origin = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallOrigin : _arrowOrigin;
                var color = (string.IsNullOrWhiteSpace(_arrowColor) ? Color.White : Calc.HexToColor(_arrowColor)) * arrowOpacity;

                // arrows should be centre aligned in each 2x2 box
                // offset by half a tile if the width or height is odd
                for (int y = 0; y < arrowsY; y++)
                {
                    int offsetY = y * 16 + 8 + heightInTiles % 2 * 4;
                    if (heightInTiles == 1) offsetY = 4;
                    for (int x = 0; x < arrowsX; x++)
                    {
                        int offsetX = x * 16 + 8 + widthInTiles % 2 * 4;
                        if (widthInTiles == 1) offsetX = 4;
                        texture.Draw(Position + _arrowShakeOffset + new Vector2(offsetX, offsetY), origin, color, 1f, 0f);
                    }
                }
            }
        }

        private bool staticMoverCollideCheck(Solid solid)
        {
            if (solid is SolidTiles) return false;
            // TODO: allow the mapper to specify the static mover collision hitbox
            // Collider = staticMoverHitbox;
            var collides = CollideCheck(solid);
            // Collider = normalHitbox;
            return collides;
        }

        private bool canConnectTo(GravityField other) =>
            other.GravityType == GravityType &&
            other.AttachToSolids == AttachToSolids;

        private IEnumerable<GravityField> getAdjacent()
        {
            if (!Scene.Tracker.Entities.ContainsKey(typeof(GravityField)))
                return Enumerable.Empty<GravityField>();

            var adjacent = new List<GravityField>();
            Scene.CollideInto(new Rectangle((int)X, (int)Y - 2, (int)Width, (int)Height + 4), adjacent);
            Scene.CollideInto(new Rectangle((int)X - 2, (int)Y, (int)Width + 4, (int)Height), adjacent);
            adjacent.Remove(this);
            adjacent.RemoveAll(f => !canConnectTo(f));
            return adjacent;
        }

        private void buildFieldGroup(GravityFieldGroup existing = null)
        {
            if (_fieldGroup != null) return;

            _fieldGroup = existing ?? new GravityFieldGroup();

            var adjacent = getAdjacent();
            foreach (var field in adjacent)
                field.buildFieldGroup(_fieldGroup);
        }

        [Tracked]
        internal class GravityFieldRenderer : ConnectedFieldRenderer<GravityField>
        {
        }

        private class GravityFieldGroup
        {
            public int Semaphore;
        }

        public enum VisualType
        {
            Default = -2,
            None = -1,
            Normal = 0,
            Inverted,
            Toggle,
        }
    }
}
