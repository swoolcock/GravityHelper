// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper
{
    public enum GravityType
    {
        None = -1,
        Normal = 0,
        Inverted,
        Toggle,
    }

    public static class GravityTypeExtensions
    {
        public static GravityType Opposite(this GravityType type) => type switch
        {
            GravityType.Normal => GravityType.Inverted,
            GravityType.Inverted => GravityType.Normal,
            _ => type,
        };

        public static bool RequiresHooks(this GravityType type) =>
            type == GravityType.Inverted || type == GravityType.Toggle;
    }
}
