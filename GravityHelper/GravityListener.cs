using System;
using Monocle;

namespace GravityHelper
{
    [Tracked(false)]
    class GravityListener : Component
    {
        public GravityListener(Action<GravityModule.GravityTypes> onChangeGravity = null) : base(true, false)
        {
            OnChangeGravity = onChangeGravity;
        }

        public Action<GravityModule.GravityTypes> OnChangeGravity;
    }
}
