// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
                    var offsetVector = projectedPoint - entity.Center;
                    var isRightOrDown = TargetOffset.Y == 0 ? offsetVector.Y > 0 : offsetVector.X > 0;

                    if (_trackedComponents.TryGetValue(gravityComponent.GlobalId, out var tracked))
                    {
                        if (tracked.CooldownRemaining <= 0 && projectedScalar >= 0 && projectedScalar <= 1 && tracked.IsRightOrDown != isRightOrDown)
                        {
                            if (entity is Player player && CancelDash && player.StateMachine.State == Player.StDash)
                                player.StateMachine.State = Player.StNormal;

                            gravityComponent.SetGravity(GravityType, MomentumMultiplier);
                            tracked.CooldownRemaining = Cooldown;
                        }

                        tracked.IsRightOrDown = isRightOrDown;
                        tracked.ProjectedScalar = projectedScalar;
                        tracked.ProjectedPoint = projectedPoint;
                        tracked.EntityCenter = entity.Center;
                    }
                    else
                    {
                        _trackedComponents[gravityComponent.GlobalId] = new ComponentTracking
                        {
                            IsRightOrDown = isRightOrDown,
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
                if (tracked.ProjectedScalar >= 0 && tracked.ProjectedScalar <= 1)
                    Draw.Line(tracked.ProjectedPoint.Round(), tracked.EntityCenter.Round(), Color.Red);
            }
        }

        private class ComponentTracking
        {
            public bool IsRightOrDown;
            public float CooldownRemaining;
            public float ProjectedScalar;
            public Vector2 ProjectedPoint;
            public Vector2 EntityCenter;
        }
    }
}
