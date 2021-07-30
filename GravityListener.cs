using System;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        public Action<GravityChangeArgs> GravityChanged;

        public GravityListener()
            : base(true, false)
        {
        }

        public override void EntityAwake()
        {
            // let GravityController handle its own Awake
            if (Entity is not GravityController)
                OnGravityChanged(new GravityChangeArgs(GravityHelperModule.Instance.Gravity));
        }

        public void OnGravityChanged(GravityChangeArgs args) =>
            GravityChanged?.Invoke(args);
    }
}
