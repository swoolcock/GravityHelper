// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/GravityTrigger")]
    public class GravityTrigger : Trigger
    {
        public bool AffectsPlayer { get; }
        public bool AffectsHoldableActors { get; }
        public bool AffectsOtherActors { get; }
        public GravityType GravityType { get; }
        public float MomentumMultiplier { get; }
        public virtual bool ShouldAffectPlayer => true;

        public GravityTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            AffectsPlayer = data.Bool("affectsPlayer", true);
            AffectsHoldableActors = data.Bool("affectsHoldableActors");
            AffectsOtherActors = data.Bool("affectsOtherActors");
            GravityType = (GravityType)data.Int("gravityType");
            MomentumMultiplier = data.Float("momentumMultiplier", 1f);

            TriggeredEntityTypes types = TriggeredEntityTypes.None;
            if (AffectsHoldableActors) types |= TriggeredEntityTypes.HoldableActors;
            if (AffectsOtherActors) types |= TriggeredEntityTypes.NonHoldableActors;

            if (GravityType != GravityType.None && (AffectsHoldableActors || AffectsOtherActors))
            {
                Add(new GravityTriggerComponent(types)
                {
                    GravityType = GravityType,
                    MomentumMultiplier = MomentumMultiplier,
                });
            }
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            HandleOnEnter(player);
        }

        protected virtual void HandleOnEnter(Player player)
        {
            if (GravityType == GravityType.None || !AffectsPlayer || !ShouldAffectPlayer)
                return;

            GravityHelperModule.PlayerComponent.SetGravity(GravityType, MomentumMultiplier);
        }

        public override void OnStay(Player player)
        {
            base.OnEnter(player);
            HandleOnStay(player);
        }

        protected virtual void HandleOnStay(Player player)
        {
            base.OnStay(player);

            if (!AffectsPlayer || !ShouldAffectPlayer ||
                GravityType == GravityType.None ||
                GravityType == GravityType.Toggle ||
                GravityType == GravityHelperModule.PlayerComponent.CurrentGravity)
                return;

            GravityHelperModule.PlayerComponent.SetGravity(GravityType, MomentumMultiplier);
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            HandleOnLeave(player);
        }

        protected virtual void HandleOnLeave(Player player)
        {
        }
    }
}
