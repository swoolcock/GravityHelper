// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities
{
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

        private string getAnimId(string id) => GravityType switch
        {
            GravityType.None => $"none_{id}",
            GravityType.Normal => $"normal_{id}",
            GravityType.Inverted => $"invert_{id}",
            GravityType.Toggle => $"toggle_{id}",
            _ => id,
        };

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly Sprite _sprite;
        private readonly StaticMover _staticMover;

        private readonly Wiggler _wiggler;
        private float _cooldownRemaining;

        private float _gravityCooldown;
        private readonly bool _showIndicator;
        private readonly bool _largeIndicator;
        private readonly int _indicatorOffset;
        private readonly bool _defaultToController;
        private IndicatorRenderer _indicatorRenderer;
        private Vector2 _indicatorShakeOffset;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly DynData<Spring> _springData;

        public static Entity LoadFloor(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Floor);

        public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Ceiling);

        public static Entity LoadWallLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallLeft);

        public static Entity LoadWallRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallRight);

        public GravitySpring(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset, (Spring.Orientations)((int)orientation % 3), data.Bool("playerCanUse", true))
        {
            _springData = new DynData<Spring>(this);

            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            PlayerCanUse = data.Bool("playerCanUse", true);
            GravityType = data.Enum<GravityType>("gravityType");

            _defaultToController = data.Bool("defaultToController");
            _gravityCooldown = data.Float("gravityCooldown", BehaviorGravityController.DEFAULT_SPRING_COOLDOWN);
            _showIndicator = data.Bool("showIndicator");
            _largeIndicator = data.Bool("largeIndicator");
            _indicatorOffset = data.Int("indicatorOffset", 8);

            // handle legacy spring settings
            if (data.TryFloat("cooldown", out var cooldown))
                _gravityCooldown = cooldown;

            Orientation = orientation;

            // get spring components
            _sprite = _springData.Get<Sprite>("sprite");
            _staticMover = _springData.Get<StaticMover>("staticMover");
            _wiggler = _springData.Get<Wiggler>("wiggler");
            var playerCollider = Get<PlayerCollider>();
            var holdableCollider = Get<HoldableCollider>();
            var pufferCollider = Get<PufferCollider>();

            // update sprite
            GFX.SpriteBank.CreateOn(_sprite, "gravitySpring");
            _sprite.Play(getAnimId("idle"));
            _sprite.Origin.X = _sprite.Width / 2f;
            _sprite.Origin.Y = _sprite.Height;

            // update callbacks
            _staticMover.OnEnable = OnEnable;
            _staticMover.OnDisable = OnDisable;
            _staticMover.OnShake = OnShake;
            playerCollider.OnCollide = OnCollide;
            holdableCollider.OnCollide = OnHoldable;
            pufferCollider.OnCollide = OnPuffer;

            // update things by orientation
            switch (orientation)
            {
                case Orientations.Floor:
                    _sprite.Rotation = 0f;
                    break;

                case Orientations.WallLeft:
                    _sprite.Rotation = (float)(Math.PI / 2f);
                    break;

                case Orientations.WallRight:
                    _sprite.Rotation = (float)(-Math.PI / 2f);
                    break;

                case Orientations.Ceiling:
                    _sprite.Rotation = (float)Math.PI;
                    Collider.Top += 6f;
                    pufferCollider.Collider.Top += 6;
                    _staticMover.SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY);
                    _staticMover.JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY);
                    break;
            }
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

        private void OnEnable()
        {
            Visible = Collidable = true;
            _sprite.Color = Color.White;
            _sprite.Play(getAnimId("idle"));
        }

        private void OnDisable()
        {
            Collidable = false;
            if (VisibleWhenDisabled)
            {
                _sprite.Play("disabled");
                _sprite.Color = DisabledColor;
            }
            else
                Visible = false;
        }

        private void OnShake(Vector2 amount) => _indicatorShakeOffset += amount;

        public override void Update()
        {
            base.Update();

            if (_indicatorRenderer != null)
                _indicatorRenderer.Visible = Visible;

            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining = Math.Max(0, _cooldownRemaining - Engine.DeltaTime);
                // TODO: update sprite to show cooldown
            }
        }

        private void OnCollide(Player player)
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
        }

        private void OnHoldable(Holdable h)
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

            // handle Theo
            if (h.Entity is TheoCrystal theoCrystal)
            {
                // do nothing if moving away
                if (theoCrystal.Speed.Y > 0) return false;
                var data = new DynData<TheoCrystal>(theoCrystal);
                theoCrystal.Speed.X *= x_multiplier;
                theoCrystal.Speed.Y = y_value;
                data["noGravityTimer"] = no_gravity_timer;
                return true;
            }

            // handle jellies
            if (h.Entity is Glider glider)
            {
                // do nothing if moving away
                if (glider.Speed.Y > 0) return false;
                var data = new DynData<Glider>(glider);
                glider.Speed.X *= x_multiplier;
                glider.Speed.Y = y_value;
                data["noGravityTimer"] = no_gravity_timer;
                data.Get<Wiggler>("wiggler").Start();
                return true;
            }

            // if the entity has a GravityComponent then try to use the speed delegates from that
            if (h.Entity.Get<GravityComponent>() is { } gravityComponent)
            {
                var speed = gravityComponent.EntitySpeed;
                // do nothing if moving away
                if (speed.Y > 0) return false;
                speed.X *= x_multiplier;
                speed.Y = y_value;
                gravityComponent.EntitySpeed = speed;
                return true;
            }

            // just take a guess that there's a Speed field if it's an unknown entity
            var entityType = h.Entity.GetType();
            var speedField = entityType.GetRuntimeFields().FirstOrDefault(f => f.Name == "Speed" && f.FieldType == typeof(Vector2));
            if (speedField != null)
            {
                var speed = (Vector2)speedField.GetValue(h.Entity);
                // do nothing if moving away
                if (speed.Y > 0) return false;
                speed.X *= x_multiplier;
                speed.Y = y_value;
                speedField.SetValue(h.Entity, speed);
                return true;
            }

            return false;
        }

        private void OnPuffer(Puffer p)
        {
            // at the moment puffers don't support gravity, so just handle ceiling springs separately
            if (Orientation == Orientations.Ceiling && !pufferHitCeilingSpring(p) ||
                Orientation != Orientations.Ceiling && !p.HitSpring(this))
                return;

            bounceAnimate();
        }

        private bool pufferHitCeilingSpring(Puffer p)
        {
            var data = new DynData<Puffer>(p);
            if (data.Get<Vector2>("hitSpeed").Y > 0)
                return false;

            ReflectionCache.Puffer_GotoHitSpeed.Invoke(p, new object[] { 224f * Vector2.UnitY });
            p.MoveTowardsX(CenterX, 4f);
            data.Get<Wiggler>("bounceWiggler").Start();
            ReflectionCache.Puffer_Alert.Invoke(p, new object[] { true, false });
            return true;
        }

        private void bounceAnimate()
        {
            Audio.Play("event:/game/general/spring", BottomCenter);
            _staticMover.TriggerPlatform();
            _sprite.Play(getAnimId("bounce"), true);
            _wiggler.Start();
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
            self.Collider = self.GetNormalHitbox();
            self.MoveV(GravityHelperModule.ShouldInvertPlayer ? self.Bottom - fromY : fromY - self.Top);
            if (!self.Inventory.NoRefills)
                self.RefillDash();
            self.RefillStamina();

            var data = DynamicData.For(self);
            data.Set("jumpGraceTimer", 0f);
            data.Set("varJumpTimer", 0f);
            data.Set("dashAttackTimer", 0.0f);
            data.Set("gliderBoostTimer", 0.0f);
            data.Set("wallSlideTimer", 1.2f);
            data.Set("wallBoostTimer", 0.0f);
            data.Set("varJumpSpeed", 0f);
            data.Set("launched", false);

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
}
