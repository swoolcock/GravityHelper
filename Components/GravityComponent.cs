// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Components
{
    [Tracked(true)]
    public class GravityComponent : Component
    {
        private static int _nextId;
        public readonly int GlobalId = _nextId++;

        internal const string INVERTED_KEY = "GravityHelper_Inverted";

        private GravityType _currentGravity;
        public GravityType CurrentGravity
        {
            get => _currentGravity;
            private set
            {
                _currentGravity = value;
                if (_data != null) _data.Data[INVERTED_KEY] = value == GravityType.Inverted;
            }
        }

        private DynData<Entity> _data;

        public bool UpdateEntity { get; set; } = true;
        public Func<bool> CheckInvert;
        public Action<GravityChangeArgs> UpdateVisuals;
        public Action<GravityChangeArgs> UpdateColliders;
        public Action<GravityChangeArgs> UpdatePosition;
        public Action<GravityChangeArgs> UpdateSpeed;

        public Func<Vector2> GetSpeed;
        public Action<Vector2> SetSpeed;

        public Vector2 EntitySpeed
        {
            get => GetSpeed?.Invoke() ?? Vector2.Zero;
            set => SetSpeed?.Invoke(value);
        }

        public bool ShouldInvert => _currentGravity == GravityType.Inverted;
        public bool ShouldInvertChecked => _currentGravity == GravityType.Inverted && (CheckInvert?.Invoke() ?? true);

        public GravityComponent()
            : base(true, false)
        {
        }

        public override void Added(Entity entity)
        {
            base.Added(entity);

            _data = new DynData<Entity>(entity);
            _data.Data[INVERTED_KEY] = _currentGravity == GravityType.Inverted;
        }

        public override void Removed(Entity entity)
        {
            base.Removed(entity);

            updateGravity(new GravityChangeArgs(GravityType.Normal, CurrentGravity));

            _data.Data[INVERTED_KEY] = false;
            _data = null;
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            triggerGravityListeners(new GravityChangeArgs(CurrentGravity));
        }

        public void SetGravity(GravityType newValue, float momentumMultiplier = 1f)
        {
            // bail for placeholder cases
            if (newValue < 0) return;

            var oldGravity = _currentGravity;
            var newGravity = newValue == GravityType.Toggle ? _currentGravity.Opposite() : newValue;
            var args = new GravityChangeArgs(newGravity, oldGravity, momentumMultiplier, newValue == GravityType.Toggle);

            CurrentGravity = newGravity;

            updateGravity(args);
            triggerGravityListeners(args);
        }

        private void updateGravity(GravityChangeArgs args)
        {
            if (!UpdateEntity) return;

            if (UpdatePosition != null)
                UpdatePosition(args);
            else if (args.Changed && Entity.Collider != null)
                Entity.Position.Y = args.NewValue == GravityType.Inverted
                    ? Entity.Collider.AbsoluteTop
                    : Entity.Collider.AbsoluteBottom;

            if (UpdateColliders != null)
                UpdateColliders(args);
            else if (args.Changed)
            {
                if (Entity.Collider != null)
                    Entity.Collider.Top = -Entity.Collider.Bottom;
                if (Entity.Get<Holdable>() is { } holdable)
                    holdable.PickupCollider.Top = -holdable.PickupCollider.Bottom;
            }

            UpdateSpeed?.Invoke(args);
            UpdateVisuals?.Invoke(args);
        }

        private void triggerGravityListeners(GravityChangeArgs args)
        {
            var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>();
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.OnGravityChanged(Entity, args);
        }
    }
}
