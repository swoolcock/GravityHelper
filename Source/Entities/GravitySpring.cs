// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity(
    "GravityHelper/GravitySpringFloor = LoadFloor",
    "GravityHelper/GravitySpringCeiling = LoadCeiling",
    "GravityHelper/GravitySpringWallLeft = LoadWallLeft",
    "GravityHelper/GravitySpringWallRight = LoadWallRight")]
public class GravitySpring : Spring
{
    // ReSharper disable once UnusedMember.Global
    public static bool RequiresHooks(EntityData data) => data.Enum<GravityType>("gravityType").RequiresHooks();

    public bool PlayerCanUse { get; }
    public new Orientations Orientation { get; }
    public GravityType GravityType { get; }
    public int RefillDashCount { get; }
    public bool RefillStamina { get; }

    private string getAnimId(string id) => GravityType switch
    {
        GravityType.None => $"none_{id}",
        GravityType.Normal => $"normal_{id}",
        GravityType.Inverted => $"invert_{id}",
        GravityType.Toggle => $"toggle_{id}",
        _ => id,
    };

    private string getOverlayAnimId(string id)
    {
        if (!RefillStamina)
            return $"no_stamina_{id}";
        return RefillDashCount switch
        {
            0 => $"no_dash_{id}",
            >= 2 => $"two_dash_{id}",
            _ => "",
        };
    }

    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    private float _cooldownRemaining;

    private float _gravityCooldown;
    private readonly bool _showIndicator;
    private readonly bool _largeIndicator;
    private readonly int _indicatorOffset;
    private readonly string _indicatorTexture;
    private readonly bool _defaultToController;
    private IndicatorRenderer _indicatorRenderer;
    private Vector2 _indicatorShakeOffset;
    private string _spriteName;
    private string _overlaySpriteName;
    private bool _showOverlay;
    private string _refillSound;

    private Sprite _overlaySprite;

    [UsedImplicitly]
    public static Entity LoadFloor(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravitySpring(entityData, offset, Orientations.Floor);

    [UsedImplicitly]
    public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravitySpring(entityData, offset, Orientations.Ceiling);

    [UsedImplicitly]
    public static Entity LoadWallLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravitySpring(entityData, offset, Orientations.WallLeft);

    [UsedImplicitly]
    public static Entity LoadWallRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravitySpring(entityData, offset, Orientations.WallRight);

    public GravitySpring(EntityData data, Vector2 offset, Orientations orientation)
        : base(data.Position + offset, (Spring.Orientations)((int)orientation % 3), data.Bool("playerCanUse", true))
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        var defaultCooldown = _pluginVersion.Major >= 2
            ? BehaviorGravityController.DEFAULT_SPRING_COOLDOWN_V2
            : BehaviorGravityController.DEFAULT_SPRING_COOLDOWN_V1;

        PlayerCanUse = data.Bool("playerCanUse", true);
        GravityType = data.Enum<GravityType>("gravityType");
        RefillDashCount = data.Int("refillDashCount", -1);
        RefillStamina = data.Bool("refillStamina", true);

        _defaultToController = data.Bool("defaultToController");
        _gravityCooldown = data.Float("gravityCooldown", defaultCooldown);
        _showIndicator = data.Bool("showIndicator");
        _largeIndicator = data.Bool("largeIndicator");
        _indicatorOffset = data.Int("indicatorOffset", 8);
        _indicatorTexture = data.Attr("indicatorTexture");
        _spriteName = data.Attr("spriteName");
        _overlaySpriteName = data.Attr("overlaySpriteName");
        _refillSound = data.Attr("refillSound");

        if (string.IsNullOrWhiteSpace(_spriteName))
            _spriteName = "gravitySpring";

        if (string.IsNullOrWhiteSpace(_overlaySpriteName))
            _overlaySpriteName = "gravitySpringOverlay";

        // showOverlay defaults to false to handle legacy springs
        _showOverlay = data.Bool("showOverlay");

        // handle legacy spring settings
        if (data.TryFloat("cooldown", out var cooldown))
            _gravityCooldown = cooldown;

        Orientation = orientation;

        // get spring components
        var playerCollider = Get<PlayerCollider>();
        var holdableCollider = Get<HoldableCollider>();
        var pufferCollider = Get<PufferCollider>();

        // update sprite
        GFX.SpriteBank.CreateOn(sprite, _spriteName);
        sprite.Play(getAnimId("idle"));

        // create overlay sprite
        if (_showOverlay)
        {
            var anim = getOverlayAnimId("idle");
            if (!string.IsNullOrWhiteSpace(anim))
            {
                Add(_overlaySprite = GFX.SpriteBank.Create(_overlaySpriteName));
                _overlaySprite.Play(anim);
            }
        }

        // update callbacks
        staticMover.OnEnable = OnEnable;
        staticMover.OnDisable = OnDisable;
        staticMover.OnShake = OnShake;
        playerCollider.OnCollide = OnCollide;
        holdableCollider.OnCollide = OnHoldable;
        pufferCollider.OnCollide = OnPuffer;

        // update things by orientation
        switch (orientation)
        {
            case Orientations.Floor:
                sprite.Rotation = 0f;
                break;

            case Orientations.WallLeft:
                sprite.Rotation = (float)(Math.PI / 2f);
                break;

            case Orientations.WallRight:
                sprite.Rotation = (float)(-Math.PI / 2f);
                break;

            case Orientations.Ceiling:
                sprite.Rotation = (float)Math.PI;
                Collider.Top += 6f;
                pufferCollider.Collider.Top += 6;
                staticMover.SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY);
                staticMover.JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY);
                break;
        }

        if (_overlaySprite != null)
            _overlaySprite.Rotation = sprite.Rotation;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (_defaultToController && Scene.GetActiveController<BehaviorGravityController>() is { } behaviorController)
        {
            _gravityCooldown = behaviorController.SpringCooldown;
        }

        if (_showIndicator && GravityType is GravityType.Normal or GravityType.Inverted or GravityType.Toggle)
        {
            scene.Add(_indicatorRenderer = new IndicatorRenderer(this));
        }
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        _indicatorRenderer?.RemoveSelf();
        _indicatorRenderer = null;
    }

    private new void OnEnable()
    {
        Visible = Collidable = true;

        sprite.Color = Color.White;
        sprite.Play(getAnimId("idle"));

        if (_overlaySprite != null)
        {
            _overlaySprite.Color = Color.White;
            _overlaySprite.Play(getOverlayAnimId("idle"));
        }
    }

    private new void OnDisable()
    {
        Collidable = false;
        if (VisibleWhenDisabled)
        {
            sprite.Play("disabled");
            sprite.Color = DisabledColor;

            if (_overlaySprite != null)
            {
                _overlaySprite.Color = Color.White;
                _overlaySprite.Play(getOverlayAnimId("idle"));
            }
        }
        else
            Visible = false;
    }

    private void OnShake(Vector2 amount) => _indicatorShakeOffset += amount;

    public override void Update()
    {
        base.Update();

        if (_overlaySprite != null)
            _overlaySprite.Scale = sprite.Scale;

        if (_indicatorRenderer != null)
            _indicatorRenderer.Visible = Visible;

        if (_cooldownRemaining > 0)
        {
            _cooldownRemaining = Math.Max(0, _cooldownRemaining - Engine.DeltaTime);
            // TODO: update sprite to show cooldown
        }
    }

    private new void OnCollide(Player player)
    {
        // ignore spring if dream dashing, if we're not allowed to use it, or if we're on cooldown
        if (player.StateMachine.State == Player.StDreamDash || !PlayerCanUse)
            return;

        // ignore spring if moving away
        var realY = GravityHelperModule.ShouldInvertPlayer ? -player.Speed.Y : player.Speed.Y;
        switch (Orientation)
        {
            case Orientations.Floor when realY < 0:
            case Orientations.Ceiling when realY > 0:
            case Orientations.WallLeft when player.Speed.X > 240:
            case Orientations.WallRight when player.Speed.X < -240:
                return;
        }

        // set gravity and cooldown if not on cooldown
        if (GravityType != GravityType.None && (_cooldownRemaining <= 0f || _gravityCooldown <= 0f))
        {
            GravityHelperModule.PlayerComponent?.SetGravity(GravityType);
            _cooldownRemaining = _gravityCooldown;
            // TODO: update sprite to show cooldown
        }

        // boing!
        bounceAnimate();

        // cache stamina and inventory
        var oldStamina = player.Stamina;
        var oldDashes = player.Dashes;

        // bounce player away
        switch (Orientation)
        {
            case Orientations.Floor:
                if (GravityHelperModule.ShouldInvertPlayer)
                    InvertedSuperBounce(player, Top);
                else
                    player.SuperBounce(Top);
                break;

            case Orientations.Ceiling:
                if (!GravityHelperModule.ShouldInvertPlayer)
                    InvertedSuperBounce(player, Bottom);
                else
                    player.SuperBounce(Bottom);
                break;

            case Orientations.WallLeft:
                player.SideBounce(1, CenterRight.X, CenterRight.Y);
                break;

            case Orientations.WallRight:
                player.SideBounce(-1, CenterLeft.X, CenterLeft.Y);
                break;
        }

        // undo stamina refill if required
        if (!RefillStamina)
            player.Stamina = oldStamina;

        // undo or override dash refill if required
        if (RefillDashCount >= 0)
        {
            player.Dashes = Math.Max(oldDashes, RefillDashCount);
            if (!string.IsNullOrWhiteSpace(_refillSound) && RefillDashCount > player.MaxDashes && oldDashes < RefillDashCount)
                Audio.Play(_refillSound, Position);
        }
    }

    private new void OnHoldable(Holdable h)
    {
        var holdableGravity = h.Entity.GetGravity();
        var relativeCeiling = holdableGravity == GravityType.Normal && Orientation == Orientations.Ceiling ||
            holdableGravity == GravityType.Inverted && Orientation == Orientations.Floor;

        if (relativeCeiling && !holdableHitCeilingSpring(h) ||
            !relativeCeiling && !h.HitSpring(this))
            return;

        bounceAnimate();

        // try to flip gravity i guess?
        if (h.Entity.Get<GravityComponent>() is { } gravityComponent)
            gravityComponent.SetGravity(GravityType);
    }

    private bool holdableHitCeilingSpring(Holdable h)
    {
        if (h.IsHeld) return false;

        const float x_multiplier = 0.5f;
        const float y_value = 160f;
        const float no_gravity_timer = 0.15f;

        // get the speed
        var speed = h.GetSpeed();

        // do nothing if moving away
        if (speed.Y > 0) return false;

        // update values
        speed.X *= x_multiplier;
        speed.Y = y_value;

        // set speed
        h.SetSpeed(speed);

        switch (h.Entity)
        {
            // handle Theo
            case TheoCrystal theoCrystal:
                theoCrystal.noGravityTimer = no_gravity_timer;
                break;

            // handle jellies
            case Glider glider:
                glider.noGravityTimer = no_gravity_timer;
                glider.wiggler.Start();
                break;
        }

        return true;
    }

    private new void OnPuffer(Puffer p)
    {
        // at the moment puffers don't support gravity, so just handle ceiling springs separately
        if (Orientation == Orientations.Ceiling && !pufferHitCeilingSpring(p) ||
            Orientation != Orientations.Ceiling && !p.HitSpring(this))
            return;

        bounceAnimate();
    }

    private bool pufferHitCeilingSpring(Puffer p)
    {
        if (p.hitSpeed.Y > 0)
            return false;

        p.GotoHitSpeed(224f * Vector2.UnitY);
        p.MoveTowardsX(CenterX, 4f);
        p.bounceWiggler.Start();
        p.Alert(true, false);
        return true;
    }

    private void bounceAnimate()
    {
        Audio.Play("event:/game/general/spring", Position);
        staticMover.TriggerPlatform();
        sprite.Play(getAnimId("bounce"), true);
        _overlaySprite?.Play(getOverlayAnimId("bounce"), true);
        wiggler.Start();
    }

    public new enum Orientations
    {
        Floor,
        WallLeft,
        WallRight,
        Ceiling,
    }

    public static void InvertedSuperBounce(Player self, float fromY)
    {
        if (self.StateMachine.State == Player.StBoost && self.CurrentBooster != null)
        {
            self.CurrentBooster.PlayerReleased();
            self.CurrentBooster = null;
        }

        Collider collider = self.Collider;
        self.Collider = self.normalHitbox;
        self.MoveV(GravityHelperModule.ShouldInvertPlayer ? self.Bottom - fromY : fromY - self.Top);
        if (!self.Inventory.NoRefills)
            self.RefillDash();
        self.RefillStamina();

        self.jumpGraceTimer = 0f;
        self.varJumpTimer = 0f;
        self.dashAttackTimer = 0.0f;
        self.gliderBoostTimer = 0.0f;
        self.wallSlideTimer = 1.2f;
        self.wallBoostTimer = 0.0f;
        self.varJumpSpeed = 0f;
        self.launched = false;

        self.StateMachine.State = Player.StNormal;
        self.AutoJump = false;
        self.AutoJumpTimer = 0.0f;
        self.Speed.X = 0.0f;
        self.Speed.Y = 185f;

        var level = self.SceneAs<Level>();
        level?.DirectionalShake(GravityHelperModule.ShouldInvertPlayer ? -Vector2.UnitY : Vector2.UnitY, 0.1f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        self.Sprite.Scale = new Vector2(0.5f, 1.5f);
        self.Collider = collider;
    }

    public class IndicatorRenderer : Entity
    {
        private readonly GravitySpring _spring;
        private readonly MTexture _arrowTexture;
        private readonly Vector2 _arrowOrigin;

        public IndicatorRenderer(GravitySpring spring)
        {
            _spring = spring;

            var prefix = _spring.GravityType switch
            {
                GravityType.Normal => "down",
                GravityType.Inverted => "up",
                _ => "double",
            };

            var size = _spring._largeIndicator ? string.Empty : "Small";

            if (!string.IsNullOrWhiteSpace(spring._indicatorTexture))
                _arrowTexture = GFX.Game[spring._indicatorTexture];
            else
                _arrowTexture = GFX.Game[$"objects/GravityHelper/gravityField/{prefix}Arrow{size}"];

            _arrowOrigin = new Vector2(_arrowTexture.Width / 2f, _arrowTexture.Height / 2f);

            Depth = Depths.FGDecals - 1;

            Active = false;
            Collidable = false;
        }

        public override void Render()
        {
            var offset = _spring._indicatorOffset;

            var position = _spring.Position + _spring.Orientation switch
            {
                Orientations.WallLeft => Vector2.UnitX * -offset,
                Orientations.WallRight => Vector2.UnitX * offset,
                Orientations.Ceiling => Vector2.UnitY * -offset,
                Orientations.Floor => Vector2.UnitY * offset,
                _ => Vector2.Zero,
            };

            position += _spring._indicatorShakeOffset;

            _arrowTexture.DrawOutline(position, _arrowOrigin);
            _arrowTexture.Draw(position, _arrowOrigin);
        }
    }
}
