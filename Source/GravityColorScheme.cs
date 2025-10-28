// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper;

public class GravityColorScheme
{
    #region Colors

    // GravityType
    public Color NormalColor;
    public Color InvertedColor;
    public Color ToggleColor;

    #endregion

    #region Particle Types

    // GravityBumper
    public readonly ParticleType P_GravityBumper_Ambience_Normal = new ParticleType(Bumper.P_Ambience);
    public readonly ParticleType P_GravityBumper_Ambience_Inverted = new ParticleType(Bumper.P_Ambience);
    public readonly ParticleType P_GravityBumper_Ambience_Toggle = new ParticleType(Bumper.P_Ambience);
    public readonly ParticleType P_GravityBumper_Launch_Normal = new ParticleType(Bumper.P_Launch);
    public readonly ParticleType P_GravityBumper_Launch_Inverted = new ParticleType(Bumper.P_Launch);
    public readonly ParticleType P_GravityBumper_Launch_Toggle = new ParticleType(Bumper.P_Launch);

    // GravityRefill
    public readonly ParticleType P_GravityRefill_Shatter = new ParticleType(Refill.P_Shatter);
    public readonly ParticleType P_GravityRefill_Regen = new ParticleType(Refill.P_Regen);
    public readonly ParticleType P_GravityRefill_Glow_Normal = new ParticleType(Refill.P_Glow);
    public readonly ParticleType P_GravityRefill_Glow_Inverted = new ParticleType(Refill.P_Glow);

    // InversionBlock
    public readonly ParticleType P_InversionBlock_NormalInverted = new ParticleType(Player.P_Split);
    public readonly ParticleType P_InversionBlock_NormalToggle = new ParticleType(Player.P_Split);
    public readonly ParticleType P_InversionBlock_InvertedToggle = new ParticleType(Player.P_Split);

    #endregion

    public bool NeedsShader;

    public Color this[GravityType type] =>
        type switch
        {
            GravityType.Normal => NormalColor,
            GravityType.Inverted => InvertedColor,
            GravityType.Toggle => ToggleColor,
            _ => Color.White
        };

    public static readonly GravityColorScheme Classic;
    public static readonly GravityColorScheme Colorblind;

    private static GravityColorScheme CreateColorScheme(Color normalColor, Color invertedColor, Color toggleColor, bool needsShader = true)
    {
        const float bumper_lightness = 0.5f;

        normalColor.ToHSV(out var normalHue, out var normalSaturation, out var normalValue);
        invertedColor.ToHSV(out var invertedHue, out var invertedSaturation, out var invertedValue);
        toggleColor.ToHSV(out var toggleHue, out var toggleSaturation, out var toggleValue);

        // these are calculated roughly based on "classic" base colours and the legacy secondary XNA colour constants
        // they will not match the variable name if other base colours are provided, but they should be used in place
        var mediumPurpleFromToggle = ColorExtensions.FromHSV(toggleHue - 41, toggleSaturation * 0.49f, toggleValue * 1.72f);
        var blueVioletFromNormal = ColorExtensions.FromHSV(normalHue + 31, normalSaturation * 0.81f, normalValue * 0.89f);
        var mediumVioletRedFromInverted = ColorExtensions.FromHSV(invertedHue - 38, invertedSaturation * 0.89f, invertedValue * 0.78f);
        var blueVioletFromToggle = ColorExtensions.FromHSV(toggleHue - 29, toggleSaturation * 0.81f, toggleValue * 1.78f);
        var violetFromToggle = ColorExtensions.FromHSV(toggleHue, toggleSaturation * 0.45f, toggleValue * 1.86f);

        return new()
        {
            NeedsShader = needsShader,
            NormalColor = normalColor,
            InvertedColor = invertedColor,
            ToggleColor = toggleColor,
            P_GravityBumper_Launch_Normal = { Color = normalColor, Color2 = normalColor.Lighter(bumper_lightness) },
            P_GravityBumper_Launch_Inverted = { Color = invertedColor, Color2 = invertedColor.Lighter(bumper_lightness) },
            P_GravityBumper_Launch_Toggle = { Color = toggleColor, Color2 = toggleColor.Lighter(bumper_lightness) },
            P_GravityBumper_Ambience_Normal = { Color = normalColor, Color2 = normalColor.Lighter(bumper_lightness) },
            P_GravityBumper_Ambience_Inverted = { Color = invertedColor, Color2 = invertedColor.Lighter(bumper_lightness) },
            P_GravityBumper_Ambience_Toggle = { Color = toggleColor, Color2 = toggleColor.Lighter(bumper_lightness) },
            P_GravityRefill_Shatter = { Color = toggleColor, Color2 = mediumPurpleFromToggle },
            P_GravityRefill_Regen = { Color = blueVioletFromToggle, Color2 = violetFromToggle },
            P_GravityRefill_Glow_Normal = { Color = normalColor, Color2 = blueVioletFromNormal },
            P_GravityRefill_Glow_Inverted = { Color = invertedColor, Color2 = mediumVioletRedFromInverted },
            P_InversionBlock_NormalInverted =
            {
                Color = normalColor, Color2 = invertedColor,
                DirectionRange = MathF.PI / 4f
            },
            P_InversionBlock_NormalToggle =
            {
                Color = normalColor, Color2 = toggleColor,
                DirectionRange = MathF.PI / 4f
            },
            P_InversionBlock_InvertedToggle =
            {
                Color = invertedColor, Color2 = toggleColor,
                DirectionRange = MathF.PI / 4f
            },
        };
    }

    static GravityColorScheme()
    {
        Classic = CreateColorScheme(Color.Blue, Color.Red, Color.Purple, false);

        // Based on a colorblind friendly palette I was given, these shades of blue/red/yellow
        // appear to be a good compromise for visibility across different types of colorblindness.
        Colorblind = CreateColorScheme(Calc.HexToColor("0064b1"), Calc.HexToColor("ec271b"), Calc.HexToColor("fff500"));
    }

    // particle type helpers

    public ParticleType GetBumperAmbienceParticleType(GravityType gravityType) =>
        gravityType switch
        {
            GravityType.Normal => P_GravityBumper_Ambience_Normal,
            GravityType.Inverted => P_GravityBumper_Ambience_Inverted,
            GravityType.Toggle => P_GravityBumper_Ambience_Toggle,
            _ => Bumper.P_Ambience,
        };

    public ParticleType GetBumperLaunchParticleType(GravityType gravityType) =>
        gravityType switch
        {
            GravityType.Normal => P_GravityBumper_Launch_Normal,
            GravityType.Inverted => P_GravityBumper_Launch_Inverted,
            GravityType.Toggle => P_GravityBumper_Launch_Toggle,
            _ => Bumper.P_Launch,
        };

    public Color GetRippleColor(GravityType gravityType) => gravityType.Color(this).Saturation(2f);
}
