// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
