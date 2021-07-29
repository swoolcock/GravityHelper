using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        public Action<GravityType, float> GravityChanged;

        public GravityListener()
            : base(true, false)
        {
        }

        public override void EntityAwake() => OnGravityChanged(GravityHelperModule.Instance.Gravity);

        public void OnGravityChanged(GravityType gravityType, float momentumMultiplier = 1f) =>
            GravityChanged?.Invoke(gravityType, momentumMultiplier);
    }
}
