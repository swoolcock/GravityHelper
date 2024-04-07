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
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

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
    public const string DEFAULT_SINGLE_USE_SOUND = "event:/new_content/game/10_farewell/glider_emancipate";
    public const int DEFAULT_PARTICLE_DENSITY = 4;

    private const float audio_muffle_seconds = 0.2f;
    private const float flash_seconds = 0.5f;
    private const float flash_multiplier = 3f;

    #endregion

    #region Entity Properties

    public VisualType ArrowType { get; }
    public VisualType FieldType { get; }
    public bool AttachToSolids { get; }
    public bool SingleUse { get; }
    public GravityType[] CassetteSequence { get; }

    #endregion

    #region Controller-Managed

    public Color FieldColor { get; private set; }
    public Color ArrowColor { get; private set; }
    public Color ParticleColor { get; private set; }
    public bool FlashOnTrigger { get; private set; }
    public string Sound { get; private set; }
    public string SingleUseSound { get; private set; }
    public bool ShowParticles { get; private set; }
    public int ParticleDensity { get; private set; }

    private readonly bool _defaultToController;
    private float _arrowOpacity;
    private float _fieldOpacity;
    private float _particleOpacity;
    private string _arrowColor;
    private string _fieldColor;
    private string _particleColor;
    private bool _flashOnTrigger;
    private string _sound;
    private string _singleUseSound;
    private bool _showParticles;
    private int _particleDensity;

    #endregion

    #region Private Helper Properties

    private GravityType arrowGravityType => ArrowType == VisualType.Default ? GravityType : (GravityType)ArrowType;
    // ReSharper disable once UnusedMember.Local
    private GravityType fieldGravityType => FieldType == VisualType.Default ? GravityType : (GravityType)FieldType;
    private bool shouldDrawArrows => !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None);
    private bool shouldDrawField => !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);

    #endregion

    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    private MTexture _arrowTexture;
    private MTexture _arrowSmallTexture;
    private Vector2 _arrowOrigin;
    private Vector2 _arrowSmallOrigin;
    private Vector2 _arrowShakeOffset;

    // ReSharper disable once NotAccessedField.Local
    private readonly Hitbox _normalHitbox;
    private readonly Hitbox _staticMoverHitbox;

    private Vector2[] _particles = Array.Empty<Vector2>();
    private readonly float[] _speeds = { 12f, 20f, 40f };
    private GravityFieldGroup _fieldGroup;
    private Vector2 _staticMoverOffset;
    private readonly CassetteComponent _cassetteComponent;

    #region Managed by owner

    private float _audioMuffleSecondsRemaining;
    private float _flashSecondsRemaining;
    private bool _alreadyUsed;
    private int _semaphore;

    #endregion

    public bool ShouldDrawField => shouldDrawField;

    // We'll always handle it ourselves to cover connected fields
    public override bool ShouldAffectPlayer => false;

    public GravityField(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        // entity properties
        AttachToSolids = data.Bool("attachToSolids");
        ArrowType = (VisualType)data.Int("arrowType", (int)VisualType.Default);
        FieldType = (VisualType)data.Int("fieldType", (int)VisualType.Default);
        SingleUse = data.Bool("singleUse");

        var cassetteSequenceString = data.Attr("cassetteSequence").Replace("|", ",");
        var cassetteIndex = data.Int("cassetteIndex", -1);

        if (!string.IsNullOrWhiteSpace(cassetteSequenceString))
        {
            CassetteSequence = cassetteSequenceString
                .Split(',')
                .Select(s =>
                {
                    if (!int.TryParse(s, out var value)) return default;
                    var type = (GravityType)value;
                    // TODO: support none and toggle once there's a nice way to visualise them
                    return type >= GravityType.Normal && type <= GravityType.Inverted ? type : default;
                })
                .ToArray();
            if (CassetteSequence.Length > 0)
            {
                Add(new CassetteListener
                {
                    DidBecomeActive = index =>
                    {
                        var newGravityType = index < 0 || index >= CassetteSequence.Length ? GravityType.None : CassetteSequence[index];
                        if (GravityType == newGravityType) return;
                        GravityType = newGravityType;
                        // hopefully calling configure here isn't too expensive
                        configure(Scene);
                        UpdateArrows();
                    },
                });
            }
        }
        else if (cassetteIndex >= 0)
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
        _singleUseSound = data.Attr("singleUseSound", DEFAULT_SINGLE_USE_SOUND);
        _showParticles = data.Bool("showParticles", true);
        _particleDensity = data.Int("particleDensity", DEFAULT_PARTICLE_DENSITY);

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

        UpdateArrows();
    }

    protected void UpdateArrows()
    {
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

                    if (!string.IsNullOrWhiteSpace(SingleUseSound))
                    {
                        var com = _fieldGroup.GetCenterOfMass() + _staticMoverOffset;
                        Audio.Play(SingleUseSound, com);
                    }

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
            _fieldGroup?.Owner?.HandleOnLeave(player);
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
            if (_fieldGroup != null)
            {
                foreach (var field in _fieldGroup.Fields)
                    field.configure(scene);
                _fieldGroup.CreateComponents(_fieldGroup.Fields);
            }
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
            _showParticles = visualController.FieldShowParticles;
            _particleDensity = visualController.FieldParticleDensity;
        }

        FieldColor = (string.IsNullOrWhiteSpace(_fieldColor) ? GravityType.Color() : Calc.HexToColor(_fieldColor)) * _fieldOpacity;
        ArrowColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_arrowColor) ? _arrowColor : DEFAULT_ARROW_COLOR);
        ParticleColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_particleColor) ? _particleColor : DEFAULT_PARTICLE_COLOR);
        FlashOnTrigger = _flashOnTrigger;
        ShowParticles = _showParticles;
        ParticleDensity = Math.Clamp(_particleDensity, 0, 8);

        if (_defaultToController && scene.GetActiveController<SoundGravityController>() is { } soundController)
        {
            if (GravityType == GravityType.Normal)
                _sound = soundController.NormalSound;
            else if (GravityType == GravityType.Inverted)
                _sound = soundController.InvertedSound;
            else if (GravityType == GravityType.Toggle)
                _sound = soundController.ToggleSound;

            _singleUseSound = soundController.SingleUseFieldSound;
        }

        Sound = _sound;
        SingleUseSound = _singleUseSound;

        _arrowShakeOffset = Vector2.Zero;

        if (shouldDrawField && ShowParticles && ParticleDensity > 0)
        {
            using var _ = new PushRandomDisposable(Scene);
            var divisor = MathF.Pow(2, 8 - ParticleDensity);
            var particleCount = (int)(Width * Height / divisor);
            var list = new List<Vector2>(particleCount);
            for (int index = 0; index < particleCount; index++)
                list.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f)));
            _particles = list.OrderBy(v => v.X).ToArray();
        }
        else
        {
            _particles = Array.Empty<Vector2>();
        }
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
        var level = SceneAs<Level>();

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
        int count = _particles.Length;

        float left = Math.Max(level.Camera.Left, X) - X;
        float top = Math.Max(level.Camera.Top, Y) - Y;
        float bottom = Math.Min(level.Camera.Bottom, Y + Height) - Y;
        float right = Math.Min(level.Camera.Right, X + Width) - X;

        for (int index = 0; index < count; index++)
        {
            if (_particles[index].X > right)
                break;
            if (_particles[index].X < left || _particles[index].Y < top || _particles[index].Y > bottom)
                continue;

            bool flip = GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && index % 2 == 1;
            Vector2 target = _particles[index] + Vector2.UnitY * _speeds[index % length] * Engine.DeltaTime * (flip ? -1 : 1);

            if (target.Y < top)
                target.Y += bottom - top;
            else if (target.Y >= bottom)
                target.Y -= bottom - top;

            _particles[index] = target;
        }

        base.Update();
    }

    public override void Render()
    {
        base.Render();

        var level = SceneAs<Level>();
        var cassetteIndex = _cassetteComponent?.CassetteIndex ?? -1;
        var cassetteState = _cassetteComponent?.CassetteState ?? CassetteStates.On;
        var cassetteOpacity = cassetteIndex < 0 || cassetteState == CassetteStates.On ? 1f : cassetteState == CassetteStates.Off ? 0.25f : 0.5f;
        var flashOpacity = _fieldGroup?.AlphaMultiplier ?? 1f;

        var opacity = cassetteOpacity * flashOpacity;

        var left = level.Camera.Left;
        var right = level.Camera.Right;
        var top = level.Camera.Top;
        var bottom = level.Camera.Bottom;

        if (Collidable && shouldDrawField && (cassetteIndex < 0 || cassetteState >= CassetteStates.On))
        {
            var color = ParticleColor * _particleOpacity * opacity;
            foreach (Vector2 particle in _particles)
            {
                var pos = Position + particle;
                if (pos.X < left) continue;
                if (pos.X > right) break;

                if (pos.Y >= top && pos.Y <= bottom)
                    Draw.Pixel.Draw(pos, Vector2.Zero, color);
            }
        }

        if (shouldDrawArrows)
        {
            int widthInTiles = (int)(Width / 8);
            int heightInTiles = (int)(Height / 8);

            // one arrow every 2 tiles, rounded down, but at least one
            int arrowsX = Math.Max(widthInTiles / 2, 1);
            int arrowsY = Math.Max(heightInTiles / 2, 1);

            // if width or height is 1, scale down the arrows
            var texture = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallTexture : _arrowTexture;
            var origin = widthInTiles == 1 || heightInTiles == 1 ? _arrowSmallOrigin : _arrowOrigin;
            var color = ArrowColor * _arrowOpacity * opacity * (Collidable ? 1 : 0.5f);
            const int padding = 32;

            // arrows should be centre aligned in each 2x2 box
            // offset by half a tile if the width or height is odd
            for (int y = 0; y < arrowsY; y++)
            {
                int offsetY = y * 16 + 8 + heightInTiles % 2 * 4;
                if (heightInTiles == 1) offsetY = 4;

                if (Position.Y + offsetY < top - padding) continue;
                if (Position.Y + offsetY > bottom + padding) break;

                for (int x = 0; x < arrowsX; x++)
                {
                    int offsetX = x * 16 + 8 + widthInTiles % 2 * 4;
                    if (widthInTiles == 1) offsetX = 4;

                    if (Position.X + offsetX < left - padding) continue;
                    if (Position.X + offsetX > right + padding) break;

                    var pos = Position + _arrowShakeOffset + new Vector2(offsetX, offsetY);
                    texture.Draw(pos, origin, color, 1f, 0f);
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

    // ReSharper disable UnusedMember.Global
    public enum VisualType
    {
        Default = -2,
        None = -1,
        Normal = 0,
        Inverted,
        Toggle,
    }
    // ReSharper restore UnusedMember.Global
}
