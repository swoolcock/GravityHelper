// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityLine")]
    public class GravityLine : Entity
    {
        public const float DEFAULT_MIN_ALPHA = 0.45f;
        public const float DEFAULT_MAX_ALPHA = 0.95f;
        public const float DEFAULT_FLASH_TIME = 0.35f;
        public const string DEFAULT_SOUND = "event:/gravityhelper/gravity_line";
        public const string DEFAULT_LINE_COLOR = "FFFFFF";

        private const float audio_muffle_seconds = 0.2f;

        public Vector2 TargetOffset { get; }
        public GravityType GravityType { get; }
        public float MomentumMultiplier { get; }
        public float Cooldown { get; }
        public bool CancelDash { get; }
        public bool DisableUntilExit { get; }
        public bool OnlyWhileFalling { get; }
        public TriggeredEntityTypes EntityTypes { get; }

        public float MinAlpha { get; private set; }
        public float MaxAlpha { get; private set; }
        public float FlashTime { get; private set; }
        public string Sound { get; private set; }
        public Color LineColor { get; private set; }

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly Dictionary<int, ComponentTracking> _trackedComponents = new();

        private readonly bool _defaultToController;
        private float _minAlpha;
        private float _maxAlpha;
        private float _flashTime;
        private string _playSound;
        private string _lineColor;

        private float _flashTimeRemaining;
        private float _audioMuffleSecondsRemaining;

        public GravityLine(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            TargetOffset = (data.FirstNodeNullable() ?? data.Position) - data.Position;
            GravityType = data.Enum("gravityType", GravityType.Toggle);
            MomentumMultiplier = data.Float("momentumMultiplier", 1f);
            Cooldown = data.Float("cooldown");
            CancelDash = data.Bool("cancelDash");
            DisableUntilExit = data.Bool("disableUntilExit");
            OnlyWhileFalling = data.Bool("onlyWhileFalling");
            Depth = Depths.Above;

            _defaultToController = data.Bool("defaultToController");
            _minAlpha = data.Float("minAlpha", DEFAULT_MIN_ALPHA);
            _maxAlpha = data.Float("maxAlpha", DEFAULT_MAX_ALPHA);
            _flashTime = data.Float("flashTime", DEFAULT_FLASH_TIME);
            _playSound = data.Attr("playSound", DEFAULT_SOUND);
            _lineColor = data.Attr("lineColor", string.Empty);

            var affectsPlayer = data.Bool("affectsPlayer", true);
            var affectsHoldableActors = data.Bool("affectsHoldableActors");
            var affectsOtherActors = data.Bool("affectsOtherActors");
            EntityTypes = affectsPlayer ? TriggeredEntityTypes.Player : TriggeredEntityTypes.None;
            if (affectsHoldableActors) EntityTypes |= TriggeredEntityTypes.HoldableActors;
            if (affectsOtherActors) EntityTypes |= TriggeredEntityTypes.NonHoldableActors;
        }

        public override void Update()
        {
            base.Update();

            if (_flashTimeRemaining > 0)
                _flashTimeRemaining -= Engine.DeltaTime;

            if (_audioMuffleSecondsRemaining > 0)
                _audioMuffleSecondsRemaining -= Engine.DeltaTime;

            var vvvvvv = Scene.GetActiveController<VvvvvvGravityController>(true);
            var components = Scene.Tracker.GetComponents<GravityComponent>();

            foreach (var component in components)
            {
                var entity = component.Entity;
                var gravityComponent = (GravityComponent)component;

                // only handle collidable entities
                if (!entity.Collidable) continue;

                // only if it's a supported entity
                if (EntityTypes.HasFlag(TriggeredEntityTypes.Player) && entity is Player ||
                    EntityTypes.HasFlag(TriggeredEntityTypes.HoldableActors) && entity is Actor && entity.Get<GravityHoldable>() != null ||
                    EntityTypes.HasFlag(TriggeredEntityTypes.NonHoldableActors) && entity is Actor && entity.Get<GravityHoldable>() == null)
                {
                    // find the projection onto the line
                    var projectedScalar = Vector2.Dot(entity.Center - Position, TargetOffset) / Vector2.Dot(TargetOffset, TargetOffset);
                    var projectedPoint = Position + TargetOffset * projectedScalar;
                    var normal = (projectedPoint - entity.Center).SafeNormalize();

                    // if the normal is really close to horizontal, snap it so that the sign will be consistent
                    if (Math.Abs(normal.Y) < 0.0001f) normal.Y = 0;
                    var angleSign = Math.Sign(normal.Angle());

                    if (_trackedComponents.TryGetValue(gravityComponent.GlobalId, out var tracked))
                    {
                        // if we crossed the line and it's not on cooldown and we're collidable
                        if (projectedScalar >= 0 && projectedScalar <= 1 && tracked.CooldownRemaining <= 0 && tracked.Collidable && angleSign != tracked.LastAngleSign)
                        {
                            // turn the line off until we leave it, if we must
                            if (DisableUntilExit)
                                tracked.Collidable = false;

                            // cancel dash if we must
                            if (entity is Player player && CancelDash && player.StateMachine.State == Player.StDash)
                                player.StateMachine.State = Player.StNormal;

                            // flip gravity or apply momentum modifier
                            var speed = gravityComponent.EntitySpeed;
                            if (!OnlyWhileFalling || speed.Y > 0)
                                gravityComponent.SetGravity(GravityType, MomentumMultiplier);
                            else
                                gravityComponent.EntitySpeed = new Vector2(speed.X, -speed.Y * MomentumMultiplier);

                            // if vvvvvv mode, set vertical speed to at least falling speed
                            if (vvvvvv?.IsVvvvvv ?? false)
                            {
                                var newY = 160f * (SceneAs<Level>().InSpace ? 0.6f : 1f);
                                gravityComponent.EntitySpeed = new Vector2(speed.X, Math.Max(newY, Math.Abs(speed.Y)));
                            }

                            if (!string.IsNullOrWhiteSpace(Sound) && _audioMuffleSecondsRemaining <= 0)
                            {
                                Audio.Play(Sound);
                                _audioMuffleSecondsRemaining = audio_muffle_seconds;
                            }

                            _flashTimeRemaining = FlashTime;

                            tracked.CooldownRemaining = Cooldown;
                        }

                        tracked.Collidable = tracked.Collidable || !entity.CollideLine(Position, Position + TargetOffset);
                        tracked.LastAngleSign = angleSign;
                        tracked.ProjectedScalar = projectedScalar;
                        tracked.ProjectedPoint = projectedPoint;
                        tracked.EntityCenter = entity.Center;
                    }
                    else
                    {
                        _trackedComponents[gravityComponent.GlobalId] = new ComponentTracking
                        {
                            LastAngleSign = angleSign,
                            CooldownRemaining = 0f,
                            ProjectedScalar = projectedScalar,
                            ProjectedPoint = projectedPoint,
                            EntityCenter = entity.Center,
                        };
                    }
                }
            }

            foreach (var tracked in _trackedComponents.Values)
            {
                if (tracked.CooldownRemaining > 0)
                    tracked.CooldownRemaining -= Engine.DeltaTime;
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (_defaultToController && Scene.GetActiveController<VisualGravityController>() is { } visualController)
            {
                _minAlpha = visualController.LineMinAlpha.Clamp(0f, 1f);
                _maxAlpha = visualController.LineMaxAlpha.Clamp(0f, 1f);
                _flashTime = visualController.LineFlashTime.ClampLower(0f);
                _lineColor = visualController.LineColor;
            }

            MinAlpha = _minAlpha.Clamp(0f, 1f);
            MaxAlpha = _maxAlpha.Clamp(0f, 1f);
            FlashTime = _flashTime.ClampLower(0f);
            LineColor = Calc.HexToColor(!string.IsNullOrWhiteSpace(_lineColor) ? _lineColor : DEFAULT_LINE_COLOR);

            if (_defaultToController && Scene.GetActiveController<SoundGravityController>() is { } soundController)
            {
                _playSound = soundController.LineSound;
            }

            Sound = _playSound;
        }

        public override void Render()
        {
            base.Render();

            var alpha = FlashTime == 0 ? MaxAlpha : Calc.LerpClamp(MinAlpha, MaxAlpha, _flashTimeRemaining / FlashTime);
            Draw.Line(Position.Round(), (Position + TargetOffset).Round(), LineColor * alpha, 2f);
        }

        public override void DebugRender(Camera camera)
        {
            base.DebugRender(camera);

            foreach (var tracked in _trackedComponents.Values)
            {
                var color = tracked.CooldownRemaining <= 0 && tracked.Collidable ? Color.Red : Color.DarkRed;
                if (tracked.ProjectedScalar >= 0 && tracked.ProjectedScalar <= 1)
                    Draw.Line(tracked.ProjectedPoint.Round(), tracked.EntityCenter.Round(), color);
            }
        }

        private class ComponentTracking
        {
            public int LastAngleSign;
            public bool Collidable = true;
            public float CooldownRemaining;
            public float ProjectedScalar;
            public Vector2 ProjectedPoint;
            public Vector2 EntityCenter;
        }
    }
}
