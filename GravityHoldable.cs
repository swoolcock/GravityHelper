// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        private float _invertTimeRemaining;

        public GravityHoldable() : base(true, false)
        {
        }

        public override void Update()
        {
            base.Update();

            var holdable = Entity.Get<Holdable>();
            var gravityComponent = Entity.Get<GravityComponent>();
            if (holdable == null || gravityComponent == null) return;

            if (holdable.IsHeld)
            {
                _invertTimeRemaining = InvertTime;
                gravityComponent.SetGravity(GravityComponent.PlayerComponent.CurrentGravity);
            }
            else if (InvertTime > 0)
            {
                _invertTimeRemaining -= Engine.DeltaTime;
                if (_invertTimeRemaining < 0)
                    gravityComponent.SetGravity(GravityType.Normal);
            }
        }
    }
}
