// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    public class GravityHoldable : Component
    {
        public const float DEFAULT_INVERT_TIME = 2.0f;

        private bool inverted;
        public bool Inverted
        {
            get => inverted;
            set
            {
                var oldValue = inverted;
                inverted = value;
                invertTimeRemaining = value ? DEFAULT_INVERT_TIME : 0f;

                if (oldValue == value) return;

                if (Entity is Actor actor)
                    actor.SetInverted(inverted);

                Entity.Collider.Top = value
                    ? -initialCollider.Bottom
                    : initialCollider.Top;

                if (Holdable != null)
                    Holdable.PickupCollider.Top = value
                        ? -initialPickupCollider.Bottom
                        : initialPickupCollider.Top;

                if (Holdable == null || !Holdable.IsHeld)
                    Entity.Position.Y += value ? -Entity.Collider.Height : Entity.Collider.Height;

                UpdateEntityVisuals?.Invoke(value);
            }
        }

        private Holdable holdable;
        public Holdable Holdable => holdable ??= Entity.Get<Holdable>();

        public Action<bool> UpdateEntityVisuals { get; set; }

        private float invertTimeRemaining;
        private Rectangle initialPickupCollider;
        private Rectangle initialCollider;

        public GravityHoldable() : base(true, false)
        {
        }

        public override void Update()
        {
            base.Update();

            if (Holdable == null) return;

            if (Holdable.IsHeld)
                Inverted = GravityHelperModule.ShouldInvert;
            else
            {
                invertTimeRemaining -= Engine.DeltaTime;
                if (invertTimeRemaining < 0)
                    Inverted = false;
            }
        }

        public override void Removed(Entity entity)
        {
            holdable = null;
            base.Removed(entity);
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            if (Holdable == null) return;

            initialCollider = Entity.Collider.ToRectangle();
            initialPickupCollider = Holdable.PickupCollider.ToRectangle();
        }
    }
}
