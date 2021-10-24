// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    [Tracked]
    public class GravityHoldable : Component
    {
        private float _invertTime = 2f;
        public float InvertTime
        {
            get => _invertTime;
            set => _invertTime = _invertTimeRemaining = value;
        }

        private bool _inverted;
        public bool Inverted
        {
            get => _inverted;
            set
            {
                var oldValue = _inverted;
                _inverted = value;
                _invertTimeRemaining = value ? InvertTime : 0f;

                if (oldValue == value) return;

                if (Entity is Actor actor)
                    actor.SetInverted(_inverted);

                Entity.Collider.Top = value
                    ? -_initialCollider.Bottom
                    : _initialCollider.Top;

                if (Holdable != null)
                    Holdable.PickupCollider.Top = value
                        ? -_initialPickupCollider.Bottom
                        : _initialPickupCollider.Top;

                if (Holdable == null || !Holdable.IsHeld)
                    Entity.Position.Y += value ? -Entity.Collider.Height : Entity.Collider.Height;

                UpdateEntityVisuals?.Invoke(value);
            }
        }

        private Holdable _holdable;
        public Holdable Holdable => _holdable ??= Entity.Get<Holdable>();

        public Action<bool> UpdateEntityVisuals { get; set; }

        private float _invertTimeRemaining;
        private Rectangle _initialPickupCollider;
        private Rectangle _initialCollider;

        public GravityHoldable() : base(true, false)
        {
        }

        public override void Update()
        {
            base.Update();

            if (Holdable == null) return;

            if (Holdable.IsHeld)
                Inverted = GravityHelperModule.ShouldInvert;
            else if (InvertTime > 0)
            {
                _invertTimeRemaining -= Engine.DeltaTime;
                if (_invertTimeRemaining < 0)
                    Inverted = false;
            }
        }

        public override void Removed(Entity entity)
        {
            _holdable = null;
            base.Removed(entity);
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            if (Holdable == null) return;

            _initialCollider = Entity.Collider.ToRectangle();
            _initialPickupCollider = Holdable.PickupCollider.ToRectangle();
        }
    }
}
