// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityLine")]
    public class GravityLine : Entity
    {
        public Vector2 TargetOffset { get; }
        public GravityType GravityType { get; }
        public float MomentumMultiplier { get; }
        public float Cooldown { get; }
        public bool CancelDash { get; }
        public bool DisableUntilExit { get; }
        public bool OnlyWhileFalling { get; }
        public TriggeredEntityTypes EntityTypes { get; }

        private readonly Dictionary<int, ComponentTracking> _trackedComponents = new();

        public GravityLine(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            TargetOffset = (data.FirstNodeNullable() ?? Position) - Position;
            GravityType = data.Enum("gravityType", GravityType.Toggle);
            MomentumMultiplier = data.Float("momentumMultiplier", 1f);
            Cooldown = data.Float("cooldown");
            CancelDash = data.Bool("cancelDash");
            DisableUntilExit = data.Bool("disableUntilExit");
            OnlyWhileFalling = data.Bool("onlyWhileFalling");
            Depth = Depths.Above;

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
                    var normal = projectedPoint - entity.Center;
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

        public override void Render()
        {
            base.Render();

            Draw.Line(Position.Round(), (Position + TargetOffset).Round(), Color.White * 0.5f, 2f);
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
