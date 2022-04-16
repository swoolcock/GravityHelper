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

        public Vector2 TargetOffset { get; }
        public GravityType GravityType { get; }
        public float MomentumMultiplier { get; }
        public float Cooldown { get; }
        public bool CancelDash { get; }
        public bool DisableUntilExit { get; }
        public bool OnlyWhileFalling { get; }
        public string PlaySound { get; }
        public TriggeredEntityTypes EntityTypes { get; }

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly Dictionary<int, ComponentTracking> _trackedComponents = new();

        private float? _minAlpha;
        private float? _maxAlpha;
        private float? _flashTime;

        private float _flashTimeRemaining;

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
            PlaySound = data.Attr("playSound", "event:/gravityhelper/gravity_line");
            Depth = Depths.Above;

            _minAlpha = data.NullableFloat("minAlpha")?.Clamp(0f, 1f);
            _maxAlpha = data.NullableFloat("maxAlpha")?.Clamp(0f, 1f);
            _flashTime = data.NullableFloat("flashTime")?.ClampLower(0f);

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

            var vvvvvv = Scene.GetActiveController<VvvvvvGravityController>();
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

                            if (!string.IsNullOrEmpty(PlaySound))
                                Audio.Play(PlaySound);

                            _flashTimeRemaining = _flashTime ?? DEFAULT_FLASH_TIME;

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

            var controller = Scene.GetActiveController<VisualGravityController>();
            _minAlpha ??= controller?.LineMinAlpha;
            _maxAlpha ??= controller?.LineMaxAlpha;
            _flashTime ??= controller?.LineFlashTime;
        }

        public override void Render()
        {
            base.Render();

            var minAlpha = _minAlpha ?? DEFAULT_MIN_ALPHA;
            var maxAlpha = _maxAlpha ?? DEFAULT_MAX_ALPHA;
            var flashTime = _flashTime ?? DEFAULT_FLASH_TIME;
            var alpha = flashTime == 0 ? maxAlpha : Calc.LerpClamp(minAlpha, maxAlpha, _flashTimeRemaining / flashTime);

            Draw.Line(Position.Round(), (Position + TargetOffset).Round(), Color.White * alpha, 2f);
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
