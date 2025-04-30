// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions;

// ReSharper disable once UnusedMember.Global
internal static class ColorExtensions
{
    public static Color Lighter(this Color color, float amount = 0.1f) => new Color(
        Calc.Clamp(color.R + amount, 0f, 1f),
        Calc.Clamp(color.G + amount, 0f, 1f),
        Calc.Clamp(color.B + amount, 0f, 1f),
        color.A);

    // ReSharper disable once UnusedMember.Global
    public static Color Darker(this Color color, float amount = 0.1f) => color.Lighter(-amount);

    public static void ToHSV(this Color color, out float hue, out float saturation, out float value)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        // Hue
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == r)
        {
            hue = 60f * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            hue = 60f * (((b - r) / delta) + 2);
        }
        else // max == b
        {
            hue = 60f * (((r - g) / delta) + 4);
        }

        if (hue < 0)
            hue += 360f;

        // Saturation
        saturation = (max == 0) ? 0 : (delta / max);

        // Value
        value = max;
    }

    public static Color FromHSV(float hue, float saturation, float value)
    {
        hue = hue.Mod(360f);
        saturation = saturation.Clamp01();
        value = value.Clamp01();

        float c = value * saturation; // Chroma
        float x = c * (1 - Math.Abs((hue / 60f) % 2 - 1));
        float m = value - c;

        float rPrime = 0, gPrime = 0, bPrime = 0;

        if (hue >= 0 && hue < 60)
        {
            rPrime = c; gPrime = x; bPrime = 0;
        }
        else if (hue >= 60 && hue < 120)
        {
            rPrime = x; gPrime = c; bPrime = 0;
        }
        else if (hue >= 120 && hue < 180)
        {
            rPrime = 0; gPrime = c; bPrime = x;
        }
        else if (hue >= 180 && hue < 240)
        {
            rPrime = 0; gPrime = x; bPrime = c;
        }
        else if (hue >= 240 && hue < 300)
        {
            rPrime = x; gPrime = 0; bPrime = c;
        }
        else if (hue >= 300 && hue < 360)
        {
            rPrime = c; gPrime = 0; bPrime = x;
        }

        byte r = (byte)(Math.Round((rPrime + m) * 255));
        byte g = (byte)(Math.Round((gPrime + m) * 255));
        byte b = (byte)(Math.Round((bPrime + m) * 255));

        return new Color(r, g, b);
    }
}
