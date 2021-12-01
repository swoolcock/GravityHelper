// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        private readonly WeakReference<Entity> _target;
        public Entity Target => _target != null && _target.TryGetTarget(out var target) ? target : null;

        public Type TargetType { get; }

        public Action<Entity, GravityChangeArgs> GravityChanged;

        public GravityListener()
            : base(true, false)
        {
        }

        public GravityListener(Entity target)
            : this()
        {
            _target = new WeakReference<Entity>(target);
        }

        public GravityListener(Type type)
            : this()
        {
            TargetType = type;
        }

        public override void EntityAwake()
        {
            base.EntityAwake();

            var component = Target is Player || TargetType == typeof(Player)
                ? GravityComponent.PlayerComponent
                : Target?.Get<GravityComponent>();

            if (component != null)
                GravityChanged?.Invoke(Target, new GravityChangeArgs(component.CurrentGravity, playerTriggered: false));
        }

        public void OnGravityChanged(Entity entity, GravityChangeArgs args)
        {
            if (entity != null && Target != null && Target != entity) return;
            if (entity != null && TargetType != null && entity.GetType() != TargetType) return;
            GravityChanged?.Invoke(entity, args);
        }
    }

    [TrackedAs(typeof(GravityListener))]
    public class PlayerGravityListener : GravityListener
    {
        public PlayerGravityListener() : base(typeof(Player))
        {
        }
    }
}
