// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components
{
    /// <summary>
    /// Acts in a similar way to a Trigger entity, but supports any entity with the specified
    /// component rather than just the Player. Implemented as a component so that it can be used by
    /// any entity rather than requiring its own.
    /// </summary>
    public class TriggerComponent<TComponent> : Component
        where TComponent : Component
    {
        private readonly List<TComponent> _trackedComponents = new();
        private readonly List<TComponent> _toRemove = new();

        public TriggeredEntityTypes TriggeredTypes { get; set; }
        public Action<TComponent> OnEnter { get; set; }
        public Action<TComponent> OnStay { get; set; }
        public Action<TComponent> OnLeave { get; set; }
        public Func<TComponent, bool> TriggerCheck { get; set; }

        public TriggerComponent()
            : base(true, false)
        {
        }

        public override void Removed(Entity entity)
        {
            base.Removed(entity);
            _trackedComponents.Clear();
        }

        public override void EntityRemoved(Scene scene)
        {
            base.EntityRemoved(scene);
            _trackedComponents.Clear();
        }

        public override void Update()
        {
            base.Update();

            // remove any old components we no longer have
            foreach (var component in _trackedComponents)
            {
                if (component.Entity == null)
                    _toRemove.Add(component);
                else if (!Entity.CollideCheck(component.Entity) || (component.Entity.Get<Holdable>()?.IsHeld ?? false))
                {
                    _toRemove.Add(component);
                    if (TriggerCheck?.Invoke(component) ?? true)
                        OnLeave?.Invoke(component);
                }
            }

            foreach (var component in _toRemove)
                _trackedComponents.Remove(component);

            _toRemove.Clear();

            // track and trigger any new components
            var components = Entity.CollideAllByComponent<TComponent>();
            foreach (var component in components)
            {
                // never apply to non-Actors
                if (component.Entity is not Actor)
                    continue;

                // only apply to Player if we should
                if (!TriggeredTypes.HasFlag(TriggeredEntityTypes.Player) && component.Entity is Player)
                    continue;

                // get holdable
                var holdable = component.Entity.Get<Holdable>();

                // don't apply if we have a holdable and don't want one
                if (holdable != null && !TriggeredTypes.HasFlag(TriggeredEntityTypes.HoldableActors))
                    continue;

                // don't apply if it's a held holdable
                if (holdable?.IsHeld ?? false)
                    continue;

                // don't apply if we want a holdable and don't have one
                if (holdable == null && !TriggeredTypes.HasFlag(TriggeredEntityTypes.NonHoldableActors))
                    continue;

                // trigger check
                var triggerCheck = TriggerCheck?.Invoke(component) ?? true;

                // if it's not already tracked, do so and call OnEnter
                if (!_trackedComponents.Contains(component))
                {
                    _trackedComponents.Add(component);
                    if (triggerCheck)
                        OnEnter?.Invoke(component);
                }

                // invoke OnStay if it exists
                if (triggerCheck)
                    OnStay?.Invoke(component);
            }
        }
    }

    [Flags]
    public enum TriggeredEntityTypes
    {
        None = 0,
        Player,
        HoldableActors,
        NonHoldableActors,

        NonPlayer = HoldableActors | NonHoldableActors,
        All = Player | NonPlayer,
    }
}
