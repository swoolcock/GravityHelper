// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper
{
    public struct GravityChangeArgs
    {
        public GravityType NewValue;
        public GravityType? OldValue;
        public float MomentumMultiplier;
        public bool PlayerTriggered;
        public bool WasToggled;

        public bool Changed => OldValue != null && NewValue != OldValue;

        public GravityChangeArgs(GravityType newValue, GravityType? oldValue = null, float momentumMultiplier = 1f, bool playerTriggered = true, bool wasToggled = false)
        {
            NewValue = newValue;
            OldValue = oldValue;
            MomentumMultiplier = momentumMultiplier;
            PlayerTriggered = playerTriggered;
            WasToggled = wasToggled;
        }
    }
}
