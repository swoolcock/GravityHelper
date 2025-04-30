// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper;

public enum GravityType
{
    None = -1,
    Normal = 0,
    Inverted,
    Toggle,
}

internal static class GravityTypeExtensions
{
    public static GravityType Opposite(this GravityType type) => type switch
    {
        GravityType.Normal => GravityType.Inverted,
        GravityType.Inverted => GravityType.Normal,
        _ => type,
    };

    public static bool RequiresHooks(this GravityType type) =>
        type == GravityType.Inverted || type == GravityType.Toggle;

    public static Color Color(this GravityType type, GravityColorScheme scheme = null)
    {
        scheme ??= GravityHelperModule.Settings.GetColorScheme();
        return type switch
        {
            GravityType.Normal => scheme.NormalColor,
            GravityType.Inverted => scheme.InvertedColor,
            GravityType.Toggle => scheme.ToggleColor,
            _ => Microsoft.Xna.Framework.Color.White,
        };
    }

    public static Color HighlightColor(this GravityType type, GravityColorScheme scheme = null)
    {
        var color = type.Color(scheme);
        color.ToHSV(out var hue, out float saturation, out var value);
        // these calculations should return colours of the old hex codes if the colour scheme is classic
        // if it's a different colour scheme, hopefully it should be the same "vibe"
        return type switch
        {
            GravityType.Normal => ColorExtensions.FromHSV(hue - 29.2f, saturation, value),
            GravityType.Inverted => ColorExtensions.FromHSV(hue - 4.9f, saturation * 0.891f, value * 0.863f),
            GravityType.Toggle => ColorExtensions.FromHSV(hue - 14.3f, saturation * 0.735f, value * 0.961f),
            _ => Microsoft.Xna.Framework.Color.White,
        };
    }
}
