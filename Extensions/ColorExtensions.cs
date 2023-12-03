// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions
{
    internal static class ColorExtensions
    {
        public static Color Lighter(this Color color, float amount = 0.1f) => new Color(
            Calc.Clamp(color.R + amount, 0f, 1f),
            Calc.Clamp(color.G + amount, 0f, 1f),
            Calc.Clamp(color.B + amount, 0f, 1f),
            color.A);

        public static Color Darker(this Color color, float amount = 0.1f) => color.Lighter(-amount);
    }
}
