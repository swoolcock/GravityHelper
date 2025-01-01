// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

    public static Color Color(this GravityType type, GravityColorScheme? scheme = null)
    {
        scheme ??= GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic;
        return type switch
        {
            GravityType.Normal => scheme.Value.NormalColor,
            GravityType.Inverted => scheme.Value.InvertedColor,
            GravityType.Toggle => scheme.Value.ToggleColor,
            _ => Microsoft.Xna.Framework.Color.White,
        };
    }

    // TODO: calculate highlight from actual colour
    public static Color HighlightColor(this GravityType type) => type switch
    {
        GravityType.Normal => Calc.HexToColor("007cff"),
        GravityType.Inverted => Calc.HexToColor("dc1828"),
        GravityType.Toggle => Calc.HexToColor("ca41f5"),
        _ => Microsoft.Xna.Framework.Color.White,
    };
}
