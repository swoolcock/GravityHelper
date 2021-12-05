// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper.Components
{
    /// <summary>
    /// Implementation of <see cref="TriggerComponent{TComponent}"/> specifically
    /// for <see cref="GravityComponent"/>s that handles changing gravity.
    /// </summary>
    public class GravityTriggerComponent : TriggerComponent<GravityComponent>
    {
        public GravityType GravityType { get; set; }
        public float MomentumMultiplier { get; set; } = 1f;

        public GravityTriggerComponent(TriggeredEntityTypes types)
        {
            TriggeredTypes = types;
            OnEnter = c => c.SetGravity(GravityType, MomentumMultiplier);
            OnStay = c =>
            {
                if ((GravityType == GravityType.Normal || GravityType == GravityType.Inverted) && c.CurrentGravity != GravityType)
                    c.SetGravity(GravityType, MomentumMultiplier);
            };
        }
    }
}
