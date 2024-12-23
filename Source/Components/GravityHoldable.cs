// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Triggers;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

[Tracked]
public class GravityHoldable : Component
{
    private float _invertTime = BehaviorGravityController.DEFAULT_HOLDABLE_RESET_TIME;
    public float InvertTime
    {
        get => _invertTime;
        set => _invertTime = _invertTimeRemaining = value;
    }

    private float _invertTimeRemaining;

    public GravityHoldable() : base(true, false)
    {
    }

    public void ResetInvertTime() => _invertTimeRemaining = InvertTime;

    public void SetGravityHeld()
    {
        if (Entity?.Get<GravityComponent>() is not { } gravityComponent) return;

        ResetInvertTime();

        var targetGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;
        if (gravityComponent.CurrentGravity != targetGravity)
            gravityComponent.SetGravity(targetGravity);
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);

        // the entity may already be part of the scene before the component is added
        if (entity.Scene != null)
            updateInvertTime(entity.Scene);

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
                GravityChanged = (_, _) => ResetInvertTime(),
            });
        }
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        updateInvertTime(scene);
    }

    public override void EntityAwake()
    {
        base.EntityAwake();

        // for now we'll assume a holdable spawn gravity trigger always inverts
        if (Entity.CollideCheck<HoldableSpawnGravityTrigger>())
            Entity.SetGravity(GravityType.Inverted);
    }

    private void updateInvertTime(Scene scene)
    {
        var controller = scene.GetActiveController<BehaviorGravityController>();
        InvertTime = controller?.HoldableResetTime ?? BehaviorGravityController.DEFAULT_HOLDABLE_RESET_TIME;
    }

    public override void Update()
    {
        base.Update();

        var holdable = Entity.Get<Holdable>();
        var gravityComponent = Entity.Get<GravityComponent>();
        if (holdable == null || gravityComponent == null) return;

        if (holdable.IsHeld)
            SetGravityHeld();
        else if (InvertTime > 0 && _invertTimeRemaining > 0 && gravityComponent.CurrentGravity == GravityType.Inverted)
        {
            // if the holdable is within a trigger/field that should keep it inverted, reset the inversion time
            if (Entity.CollideCheckWhere<GravityTrigger>(f => f.GravityType == GravityType.Inverted && f.AffectsHoldableActors))
            {
                ResetInvertTime();
                return;
            }
            _invertTimeRemaining -= Engine.DeltaTime;
            if (_invertTimeRemaining <= 0)
                gravityComponent.SetGravity(GravityType.Normal);
        }
    }
}
