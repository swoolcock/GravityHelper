// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Celeste.Mod.GravityHelper.Entities;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components
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

        public void ResetInvertTime() => _invertTimeRemaining = InvertTime;

        public override void Added(Entity entity)
        {
            base.Added(entity);

            entity.Add(new GravityListener(entity)
            {
                GravityChanged = (_, _) => ResetInvertTime(),
            });
        }

        public override void EntityAdded(Scene scene)
        {
            base.EntityAdded(scene);

            var controller = Scene.Entities.ToAdd.Concat(Scene.Entities).OfType<GravityController>().FirstOrDefault();
            if (controller != null)
                InvertTime = controller.HoldableResetTime;
        }

        public override void Update()
        {
            base.Update();

            var holdable = Entity.Get<Holdable>();
            var gravityComponent = Entity.Get<GravityComponent>();
            if (holdable == null || gravityComponent == null) return;

            if (holdable.IsHeld)
            {
                ResetInvertTime();
                gravityComponent.SetGravity(GravityHelperModule.PlayerComponent.CurrentGravity);
            }
            else if (InvertTime > 0 && _invertTimeRemaining > 0 && gravityComponent.CurrentGravity == GravityType.Inverted)
            {
                _invertTimeRemaining -= Engine.DeltaTime;
                if (_invertTimeRemaining <= 0)
                    gravityComponent.SetGravity(GravityType.Normal);
            }
        }
    }
}
