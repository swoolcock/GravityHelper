// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper
{
    public struct GravityChangeArgs
    {
        public GravityType NewValue;
        public GravityType OldValue;
        public float MomentumMultiplier;
        public bool WasToggled;

        public bool Changed => NewValue != OldValue;

        public GravityChangeArgs(GravityType newValue, float momentumMultiplier = 1f, bool wasToggled = false)
        {
            NewValue = newValue;
            OldValue = newValue;
            MomentumMultiplier = momentumMultiplier;
            WasToggled = wasToggled;
        }
    }
}
