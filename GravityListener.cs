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

        public override void EntityAwake() =>
            OnGravityChanged(new GravityChangeArgs(GravityHelperModule.Instance.Gravity, playerTriggered: false));

        public void OnGravityChanged(GravityChangeArgs args) =>
            GravityChanged?.Invoke(args);
    }
}
