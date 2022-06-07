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
        public const string DEFAULT_ARROW_COLOR = "FFFFFF";
        public const string DEFAULT_PARTICLE_COLOR = "FFFFFF";

        private const float audio_muffle_seconds = 0.2f;

        #region Entity Properties

        public VisualType ArrowType { get; }
        public VisualType FieldType { get; }
        public bool AttachToSolids { get; }

        #endregion

        // We'll always handle it ourselves to cover connected fields
        public override bool ShouldAffectPlayer => false;

        public Color FieldColor { get; private set; }
        public Color ArrowColor { get; private set; }
        public Color ParticleColor { get; private set; }
        public string Sound { get; private set; }

        private GravityType arrowGravityType => ArrowType == VisualType.Default ? GravityType : (GravityType) ArrowType;
        private GravityType fieldGravityType => FieldType == VisualType.Default ? GravityType : (GravityType) FieldType;

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private bool shouldDrawArrows => !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None);
        private bool shouldDrawField => !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);

        private MTexture _arrowTexture;
        private MTexture _arrowSmallTexture;
        private Vector2 _arrowOrigin;
        private Vector2 _arrowSmallOrigin;
        private Vector2 _arrowShakeOffset;

        private readonly Hitbox _normalHitbox;
        private readonly Hitbox _staticMoverHitbox;

        private readonly List<Vector2> _particles = new List<Vector2>();
        private readonly float[] _speeds = {12f, 20f, 40f};
        private GravityFieldGroup _fieldGroup;

        private readonly bool _defaultToController;
        private float _arrowOpacity;
        private float _fieldOpacity;
        private float _particleOpacity;
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

            _defaultToController = data.Bool("defaultToController");
            _arrowOpacity = data.Float("arrowOpacity", DEFAULT_ARROW_OPACITY).Clamp(0f, 1f);
            _particleOpacity = data.Float("particleOpacity", DEFAULT_PARTICLE_OPACITY).Clamp(0f, 1f);
            _fieldOpacity = data.Float("fieldOpacity", DEFAULT_FIELD_OPACITY).Clamp(0f, 1f);
            _arrowColor = data.Attr("arrowColor", DEFAULT_ARROW_COLOR);
            _fieldColor = data.Attr("fieldColor", string.Empty);
            _particleColor = data.Attr("particleColor", DEFAULT_PARTICLE_COLOR);
            _sound = data.Attr("sound", string.Empty);

            Collider = _normalHitbox = new Hitbox(data.Width, data.Height);

            _staticMoverHitbox = new Hitbox(data.Width + 2, data.Height + 2, -1, -1);

            if (AttachToSolids)
            {
                Add(new StaticMover
                {
                    OnAttach = p => Depth = p.Depth - 1,
                    SolidChecker = staticMoverCollideCheck,
                    OnShake = staticMoverOnShake,
                    OnMove = staticMoverOnMove,
                    OnEnable = () => staticMoverOnEnableDisable(true),
                    OnDisable = () => staticMoverOnEnableDisable(false),
                });
            }

            updateProperties(null);
        }

        protected override void HandleOnEnter(Player player)
        {
            if (GravityType == GravityType.None || !AffectsPlayer || _fieldGroup == null) return;

            _fieldGroup.Semaphore++;

            if (_fieldGroup.Semaphore == 1 && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                var previousGravity = playerComponent.CurrentGravity;
                playerComponent.SetGravity(GravityType, MomentumMultiplier);
                if (!string.IsNullOrWhiteSpace(Sound) && playerComponent.CurrentGravity != previousGravity && _audioMuffleSecondsRemaining <= 0)
                {
                    Audio.Play(Sound);
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

        private void updateProperties(Scene scene)
        {
            Visible = shouldDrawArrows || shouldDrawField;

            if (shouldDrawArrows && !shouldDrawField)
                Depth = Depths.FGDecals - 1;
            else
                Depth = Depths.Player + 1;

            if (shouldDrawArrows)
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

            if (shouldDrawField && scene != null)
            {
                this.GetConnectedFieldRenderer<GravityFieldRenderer, GravityField>(scene, true);
            }

            if (shouldDrawField && !_particles.Any())
            {
                for (int index = 0; index < Width * (double) Height / 16.0; ++index)
                    _particles.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f)));
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (_defaultToController && Scene.GetActiveController<VisualGravityController>() is { } visualController)
            {
                _arrowOpacity = visualController.FieldArrowOpacity.Clamp(0f, 1f);
                _fieldOpacity = visualController.FieldBackgroundOpacity.Clamp(0f, 1f);
                _particleOpacity = visualController.FieldParticleOpacity.Clamp(0f, 1f);
                _arrowColor = visualController.FieldArrowColor;
                _particleColor = visualController.FieldParticleColor;
                _fieldColor = GravityType switch
                {
                    GravityType.Normal => visualController.FieldNormalColor,
                    GravityType.Inverted => visualController.FieldInvertedColor,
                    GravityType.Toggle => visualController.FieldToggleColor,
                    _ => null,
                };
            }

            FieldColor = (string.IsNullOrWhiteSpace(_fieldColor) ? GravityType.Color() : Calc.HexToColor(_fieldColor)) * _fieldOpacity;
            ArrowColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_arrowColor) ? _arrowColor : DEFAULT_ARROW_COLOR);
            ParticleColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_particleColor) ? _particleColor : DEFAULT_PARTICLE_COLOR);

            if (_defaultToController && Scene.GetActiveController<SoundGravityController>() is { } soundController)
            {
                if (GravityType == GravityType.Normal)
                    _sound = soundController.NormalSound;
                else if (GravityType == GravityType.Inverted)
                    _sound = soundController.InvertedSound;
                else if (GravityType == GravityType.Toggle)
                    _sound = soundController.ToggleSound;
            }

            Sound = _sound;

            _arrowShakeOffset = Vector2.Zero;
            _fieldGroup = null;

            updateProperties(scene);
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            if (shouldDrawField)
                this.GetConnectedFieldRenderer<GravityFieldRenderer, GravityField>(scene, false);

            _fieldGroup.Fields.Clear();
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

            if (shouldDrawField)
            {
                var color = ParticleColor * _particleOpacity;
                foreach (Vector2 particle in _particles)
                    Draw.Pixel.Draw(Position + particle, Vector2.Zero, color);
            }

            if (shouldDrawArrows)
            {
                int widthInTiles = (int) (Width / 8);
                int heightInTiles = (int) (Height / 8);

                // one arrow every 2 tiles, rounded down, but at least one
                int arrowsX = Math.Max(widthInTiles / 2, 1);
                int arrowsY = Math.Max(heightInTiles / 2, 1);

                // if width or height is 1, scale down the arrows
                var texture = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallTexture : _arrowTexture;
                var origin = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallOrigin : _arrowOrigin;
                var color = ArrowColor * _arrowOpacity;

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
            var collider = Collider;

            // only attach to adjacent solids if this affects an actor, otherwise require overlap
            // this ensures that overlays will only attach to the entity they collide with, and not adjacent solids
            if (!AffectsNothing) Collider = _staticMoverHitbox;

            var collides = CollideCheck(solid);
            Collider = collider;
            return collides;
        }

        private void staticMoverOnShake(Vector2 amount)
        {
            foreach (var field in _fieldGroup?.Fields ?? Enumerable.Empty<GravityField>())
            {
                field._arrowShakeOffset += amount;
            }
        }

        private void staticMoverOnMove(Vector2 amount)
        {
            foreach (var field in _fieldGroup?.Fields ?? Enumerable.Empty<GravityField>())
            {
                field.Position += amount;
            }
        }

        private void staticMoverOnEnableDisable(bool enabled)
        {
            foreach (var field in _fieldGroup?.Fields ?? Enumerable.Empty<GravityField>())
            {
                field.Visible = field.Active = field.Collidable = enabled;
            }
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
            _fieldGroup.Fields.Add(this);

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
            public List<GravityField> Fields = new();
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
