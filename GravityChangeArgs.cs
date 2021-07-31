// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper
{
    public struct GravityChangeArgs
    {
        public GravityType NewValue;
        public GravityType OldValue;
        public GravityType SourceValue;

        public float MomentumMultiplier;
        public bool PlayerTriggered;

        public bool Changed => NewValue != OldValue;
        public bool WasToggled => SourceValue == GravityType.Toggle;

        public GravityChangeArgs(GravityType newValue, float momentumMultiplier = 1f, bool playerTriggered = true)
        {
            NewValue = OldValue = SourceValue = newValue;
            MomentumMultiplier = momentumMultiplier;
            PlayerTriggered = playerTriggered;
        }
    }
}
