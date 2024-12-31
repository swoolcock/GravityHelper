// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper;

public struct GravityColorScheme
{
    public Color NormalColor;
    public Color InvertedColor;
    public Color ToggleColor;

    public Color this[GravityType type] =>
        type switch
        {
            GravityType.Normal => NormalColor,
            GravityType.Inverted => InvertedColor,
            GravityType.Toggle => ToggleColor,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static readonly GravityColorScheme Classic;
    public static readonly GravityColorScheme Colorblind;

    static GravityColorScheme()
    {
        Classic = new()
        {
            NormalColor = Color.Blue,
            InvertedColor = Color.Red,
            ToggleColor = Color.Purple,
        };

        // Based on a colorblind friendly palette I was given, these shades of blue/red/yellow
        // appear to be a good compromise for visibility across different types of colorblindness.
        Colorblind = new()
        {
            NormalColor = Calc.HexToColor("0064b1"),
            InvertedColor = Calc.HexToColor("ec271b"),
            ToggleColor = Calc.HexToColor("fff500"),
        };
    }
}
