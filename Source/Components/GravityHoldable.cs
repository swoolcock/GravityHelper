// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Triggers;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

[Tracked]
public class GravityHoldable : Component
{
    [Obsolete("Use ResetTime and ResetType instead.")]
    public float InvertTime { get; set; }

    private float _sceneResetTime = BehaviorGravityController.DEFAULT_HOLDABLE_RESET_TIME;
    private float _resetTimeRemaining;
    private float? _resetTime;
    public float ResetTime
    {
        get => _resetTime ?? _sceneResetTime;
        set
        {
            _resetTime = value < 0 ? null : value;
            _resetTimeRemaining = ResetTime;
        }
    }

    public GravityType ResetType { get; set; } = GravityType.Normal;

    public GravityHoldable() : base(true, false)
    {
    }

    public void ResetTimer() => _resetTimeRemaining = ResetTime;

    public void SetGravityHeld()
    {
        if (Entity?.Get<GravityComponent>() is not { } gravityComponent) return;

        ResetTimer();

        var targetGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;
        if (gravityComponent.CurrentGravity != targetGravity)
            gravityComponent.SetGravity(targetGravity);
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);

        // the entity may already be part of the scene before the component is added
        if (entity.Scene != null)
            updateResetTime(entity.Scene);

        if (entity.Get<GravityComponent>() == null && entity.Get<Holdable>() is { } holdable)
        {
            entity.Add(new GravityComponent
            {
                GetSpeed = holdable.GetSpeed,
                SetSpeed = holdable.SetSpeed,
            });
        }

        if (entity.Get<GravityListener>() == null)
        {
            entity.Add(new GravityListener(entity)
            {
                GravityChanged = (_, _) => ResetTimer(),
            });
        }
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        updateResetTime(scene);
    }

    public override void EntityAwake()
    {
        base.EntityAwake();

        // for now we'll assume a holdable spawn gravity trigger always inverts
        if (Entity.CollideCheck<HoldableSpawnGravityTrigger>())
            Entity.SetGravity(GravityType.Inverted);
    }

    private void updateResetTime(Scene scene)
    {
        var controller = scene.GetActiveController<BehaviorGravityController>();
        _sceneResetTime = controller?.HoldableResetTime ?? BehaviorGravityController.DEFAULT_HOLDABLE_RESET_TIME;
        _resetTimeRemaining = _sceneResetTime;
    }

    public override void Update()
    {
        base.Update();

        var holdable = Entity.Get<Holdable>();
        var gravityComponent = Entity.Get<GravityComponent>();
        if (holdable == null || gravityComponent == null) return;

        if (holdable.IsHeld)
            SetGravityHeld();
        else if (ResetTime > 0 && _resetTimeRemaining > 0)
        {
            // if the holdable is within a trigger/field that should keep it something other than the reset type, reset the inversion timer
            if (Entity.CollideCheckWhere<GravityTrigger>(f => f.GravityType != ResetType && f.AffectsHoldableActors))
            {
                ResetTimer();
                return;
            }

            _resetTimeRemaining -= Engine.DeltaTime;
            if (_resetTimeRemaining <= 0)
                gravityComponent.SetGravity(ResetType);
        }
    }
}
