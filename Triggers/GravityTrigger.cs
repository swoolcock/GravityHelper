// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/GravityTrigger")]
    public class GravityTrigger : Trigger
    {
        public GravityType GravityType { get; }
        public float MomentumMultiplier { get; }

        public GravityTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            GravityType = (GravityType)data.Int("gravityType");
            MomentumMultiplier = data.Float("momentumMultiplier", 1f);
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            GravityHelperModule.PlayerComponent.SetGravity(GravityType, MomentumMultiplier);
        }
    }
}
