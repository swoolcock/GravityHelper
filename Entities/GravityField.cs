// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
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
        #region Constants

        public const float DEFAULT_ARROW_OPACITY = 0.5f;
        public const float DEFAULT_FIELD_OPACITY = 0.15f;
        public const float DEFAULT_PARTICLE_OPACITY = 0.5f;
        public const string DEFAULT_ARROW_COLOR = "FFFFFF";
        public const string DEFAULT_PARTICLE_COLOR = "FFFFFF";

        private const float audio_muffle_seconds = 0.2f;
        private const float flash_seconds = 0.5f;
        private const float flash_multiplier = 3f;

        #endregion

        #region Entity Properties

        public VisualType ArrowType { get; }
        public VisualType FieldType { get; }
        public bool AttachToSolids { get; }
        public bool SingleUse { get; }

        #endregion

        #region Controller-Managed

        public Color FieldColor { get; private set; }
        public Color ArrowColor { get; private set; }
        public Color ParticleColor { get; private set; }
        public bool FlashOnTrigger { get; private set; }
        public string Sound { get; private set; }

        private readonly bool _defaultToController;
        private float _arrowOpacity;
        private float _fieldOpacity;
        private float _particleOpacity;
        private string _arrowColor;
        private string _fieldColor;
        private string _particleColor;
        private bool _flashOnTrigger;
        private string _sound;

        #endregion

        #region Private Helper Properties

        private GravityType arrowGravityType => ArrowType == VisualType.Default ? GravityType : (GravityType) ArrowType;
        private GravityType fieldGravityType => FieldType == VisualType.Default ? GravityType : (GravityType) FieldType;
        private bool shouldDrawArrows => !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None);
        private bool shouldDrawField => !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);

        #endregion

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

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
        private Vector2 _staticMoverOffset;
        private CassetteComponent _cassetteComponent;

        #region Managed by owner

        private float _audioMuffleSecondsRemaining;
        private float _flashSecondsRemaining;
        private bool _alreadyUsed;
        private int _semaphore;

        #endregion

        // We'll always handle it ourselves to cover connected fields
        public override bool ShouldAffectPlayer => false;

        public GravityField(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            // entity properties
            AttachToSolids = data.Bool("attachToSolids");
            ArrowType = (VisualType)data.Int("arrowType", (int) VisualType.Default);
            FieldType = (VisualType)data.Int("fieldType", (int) VisualType.Default);
            SingleUse = data.Bool("singleUse");

            var cassetteIndex = data.Int("cassetteIndex", -1);
            if (cassetteIndex >= 0)
            {
                Add(_cassetteComponent = new CassetteComponent(cassetteIndex, data)
                {
                    OnStateChange = value => Collidable = value >= CassetteStates.On,
                });
            }

            // controller managed
            _defaultToController = data.Bool("defaultToController");
            _arrowOpacity = data.Float("arrowOpacity", DEFAULT_ARROW_OPACITY).Clamp(0f, 1f);
            _particleOpacity = data.Float("particleOpacity", DEFAULT_PARTICLE_OPACITY).Clamp(0f, 1f);
            _fieldOpacity = data.Float("fieldOpacity", DEFAULT_FIELD_OPACITY).Clamp(0f, 1f);
            _arrowColor = data.Attr("arrowColor", DEFAULT_ARROW_COLOR);
            _fieldColor = data.Attr("fieldColor", string.Empty);
            _particleColor = data.Attr("particleColor", DEFAULT_PARTICLE_COLOR);
            _flashOnTrigger = data.Bool("flashOnTrigger", true);
            _sound = data.Attr("sound", string.Empty);

            Collider = _normalHitbox = new Hitbox(data.Width, data.Height);

            Depth = Depths.Player + 1;

            _staticMoverHitbox = new Hitbox(data.Width + 2, data.Height + 2, -1, -1);

            if (AttachToSolids)
            {
                Add(new StaticMover
                {
                    OnAttach = p => _fieldGroup?.Fields.ForEach(f => f.Depth = p.Depth - 1),
                    SolidChecker = staticMoverCollideCheck,
                    OnShake = amount => _fieldGroup?.Fields.ForEach(f => f._arrowShakeOffset += amount),
                    OnMove = amount => _fieldGroup?.Fields.ForEach(f =>
                    {
                        f.Position += amount;
                        f._staticMoverOffset += amount;
                    }),
                    OnEnable = () => _fieldGroup?.Fields.ForEach(f => f.Active = f.Collidable = f.Visible = !SingleUse || !_fieldGroup.Owner._alreadyUsed),
                    OnDisable = () => _fieldGroup?.Fields.ForEach(f => f.Active = f.Collidable = f.Visible = false),
                });
            }

            Visible = shouldDrawArrows || shouldDrawField;

            if (shouldDrawArrows && !shouldDrawField)
                Depth = Depths.FGDecals - 1;

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

            if (shouldDrawField && !_particles.Any())
            {
                for (int index = 0; index < Width * (double) Height / 16.0; ++index)
                    _particles.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f)));
            }
        }

        protected override void HandleOnEnter(Player player)
        {
            // defer to the owner
            if (_fieldGroup?.Owner != this)
            {
                _fieldGroup?.Owner.HandleOnEnter(player);
                return;
            }

            if (_alreadyUsed || GravityType == GravityType.None || !AffectsPlayer) return;

            _semaphore++;

            if (_semaphore == 1 && GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                var previousGravity = playerComponent.CurrentGravity;
                playerComponent.SetGravity(GravityType, MomentumMultiplier);

                if (playerComponent.CurrentGravity != previousGravity)
                {
                    if (!string.IsNullOrWhiteSpace(Sound) && _audioMuffleSecondsRemaining <= 0)
                    {
                        Audio.Play(Sound);
                        _audioMuffleSecondsRemaining = audio_muffle_seconds;
                    }

                    if (FlashOnTrigger)
                    {
                        _flashSecondsRemaining = flash_seconds;
                    }

                    if (SingleUse)
                    {
                        _alreadyUsed = true;
                        _semaphore = 0;

                        var com = _fieldGroup.GetCenterOfMass() + _staticMoverOffset;
                        Audio.Play("event:/new_content/game/10_farewell/glider_emancipate", com);
                        _fieldGroup.Fields.ForEach(f => f.Active = f.Collidable = f.Visible = false);
                    }
                }
            }
        }

        protected override void HandleOnStay(Player player)
        {
            if (GravityType == GravityType.None || !AffectsPlayer || GravityType == GravityType.Toggle)
                return;

            if (GravityType != GravityHelperModule.PlayerComponent?.CurrentGravity)
                GravityHelperModule.PlayerComponent?.SetGravity(GravityType, MomentumMultiplier);
        }

        protected override void HandleOnLeave(Player player)
        {
            // defer to the owner
            if (_fieldGroup?.Owner != this)
            {
                _fieldGroup?.Owner.HandleOnLeave(player);
                return;
            }

            _semaphore--;

            if (_semaphore == 0 &&
                GravityHelperModule.PlayerComponent is { } playerComponent &&
                ExitGravityType != GravityType.None &&
                ExitGravityType != playerComponent.CurrentGravity)
            {
                playerComponent.SetGravity(ExitGravityType, MomentumMultiplier);
                // TODO: sound?
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (_fieldGroup == null)
            {
                scene.Add(buildFieldGroup(scene));
                foreach (var field in _fieldGroup.Fields)
                    field.configure(scene);
                _fieldGroup.CreateComponents(_fieldGroup.Fields);
            }
        }

        private void configure(Scene scene)
        {
            if (_defaultToController && scene.GetActiveController<VisualGravityController>() is { } visualController)
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
                _flashOnTrigger = visualController.FieldFlashOnTrigger;
            }

            FieldColor = (string.IsNullOrWhiteSpace(_fieldColor) ? GravityType.Color() : Calc.HexToColor(_fieldColor)) * _fieldOpacity;
            ArrowColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_arrowColor) ? _arrowColor : DEFAULT_ARROW_COLOR);
            ParticleColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_particleColor) ? _particleColor : DEFAULT_PARTICLE_COLOR);
            FlashOnTrigger = _flashOnTrigger;

            if (_defaultToController && scene.GetActiveController<SoundGravityController>() is { } soundController)
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
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            _fieldGroup?.Fields.Clear();
            _fieldGroup?.RemoveSelf();
            _fieldGroup = null;
        }

        public override void Update()
        {
            // only the owner should update these values
            if (_fieldGroup?.Owner == this)
            {
                if (_audioMuffleSecondsRemaining > 0)
                    _audioMuffleSecondsRemaining -= Engine.DeltaTime;
                if (_flashSecondsRemaining > 0)
                    _flashSecondsRemaining -= Engine.DeltaTime;
                _fieldGroup.AlphaMultiplier = Calc.LerpClamp(1f, flash_multiplier, _flashSecondsRemaining / flash_seconds);
            }

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

            var cassetteIndex = _cassetteComponent?.CassetteIndex ?? -1;
            var cassetteState = _cassetteComponent?.CassetteState ?? CassetteStates.On;
            var cassetteOpacity = cassetteIndex < 0 || cassetteState == CassetteStates.On ? 1f : cassetteState == CassetteStates.Off ? 0.25f : 0.5f;
            var flashOpacity = _fieldGroup?.AlphaMultiplier ?? 1f;

            var opacity = cassetteOpacity * flashOpacity;

            if (shouldDrawField && (cassetteIndex < 0 || cassetteState >= CassetteStates.On))
            {
                var color = ParticleColor * _particleOpacity * opacity;
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
                var color = ArrowColor * _arrowOpacity * opacity;

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

        /// <summary>
        /// Gets all the fields that are within two pixels of this field in any direction.
        /// </summary>
        private IEnumerable<GravityField> getAdjacent(Scene scene)
        {
            var allFields = scene.Entities.ToAdd.Concat(scene.Entities).OfType<GravityField>().ToArray();
            if (allFields.Length == 0)
                return Enumerable.Empty<GravityField>();

            var tallRect = new Rectangle((int)X, (int)Y - 2, (int)Width, (int)Height + 4);
            var wideRect = new Rectangle((int)X - 2, (int)Y, (int)Width + 4, (int)Height);

            return allFields.Where(f =>
                f != this &&
                f.GravityType == GravityType &&
                (f.CollideRect(tallRect) || f.CollideRect(wideRect)));
        }

        /// <summary>
        /// Creates and returns a <see cref="GravityFieldGroup"/>, recursively building the
        /// list of adjacent fields (retrieved with <see cref="getAdjacent"/>.
        /// The field that initially creates the group will be flagged as the "owner".
        /// </summary>
        private GravityFieldGroup buildFieldGroup(Scene scene, GravityFieldGroup existing = null)
        {
            if (_fieldGroup != null) return _fieldGroup;

            _fieldGroup = existing ?? new GravityFieldGroup(this);
            _fieldGroup.Fields.Add(this);

            foreach (var field in getAdjacent(scene))
                field.buildFieldGroup(scene, _fieldGroup);

            return _fieldGroup;
        }

        [Tracked]
        internal class GravityFieldGroup : ConnectedFieldRenderer<GravityField>
        {
            public readonly List<GravityField> Fields = new();
            public readonly GravityField Owner;

            public GravityFieldGroup(GravityField owner)
            {
                Owner = owner;
            }
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
