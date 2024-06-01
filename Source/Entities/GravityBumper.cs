// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/GravityBumper")]
public class GravityBumper : Bumper
{
    // ReSharper disable InconsistentNaming
    private static ParticleType P_Ambience_Normal;
    private static ParticleType P_Ambience_Inverted;
    private static ParticleType P_Ambience_Toggle;
    private static ParticleType P_Launch_Normal;
    private static ParticleType P_Launch_Inverted;
    private static ParticleType P_Launch_Toggle;
    // ReSharper restore InconsistentNaming

    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    public GravityType GravityType { get; }
    public bool IgnoreCoreMode { get; }
    public bool SingleUse { get; }
    public bool Static { get; }

    private readonly Sprite _rippleSprite;

    public GravityBumper(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        GravityType = (GravityType)data.Int("gravityType");
        IgnoreCoreMode = data.Bool("ignoreCoreMode");
        SingleUse = data.Bool("singleUse");

        if (IgnoreCoreMode && Get<CoreModeListener>() is { } coreModeListener)
            Remove(coreModeListener);

        sine.Rate = data.Float("wobbleRate", 1f);
        Static = data.Bool("static", false);

        // if we have a wobble rate of 0 and plugin version >= 2, force static
        if (sine.Rate == 0 && _pluginVersion.Major >= 2)
            Static = true;

        // static bumpers should totally reset the sine wave
        if (Static)
        {
            sine.Counter = 0;
            sine.Rate = 0;
            sine.Frequency = 0;
            sine.Active = false;
        }

        if (GravityType != GravityType.None)
        {
            // replace default bumper sprite
            var id = GravityType switch
            {
                GravityType.Normal => "gravityBumperNormal",
                GravityType.Inverted => "gravityBumperInvert",
                GravityType.Toggle => "gravityBumperToggle",
                _ => ""
            };
            GFX.SpriteBank.CreateOn(sprite, id);

            Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
            _rippleSprite.Color = GravityType.HighlightColor();
            _rippleSprite.Play("loop");
        }
    }

    public ParticleType GetAmbientParticleType()
    {
        if (GravityType == GravityType.None) return P_Ambience;

        if (P_Ambience_Normal == null)
        {
            const float lightness = 0.5f;
            P_Ambience_Normal = new ParticleType(P_Ambience)
            {
                Color = GravityType.Normal.Color(),
                Color2 = GravityType.Normal.Color().Lighter(lightness),
            };
            P_Ambience_Inverted = new ParticleType(P_Ambience)
            {
                Color = GravityType.Inverted.Color(),
                Color2 = GravityType.Inverted.Color().Lighter(lightness),
            };
            P_Ambience_Toggle = new ParticleType(P_Ambience)
            {
                Color = GravityType.Toggle.Color(),
                Color2 = GravityType.Toggle.Color().Lighter(lightness),
            };
        }

        return GravityType switch
        {
            GravityType.Normal => P_Ambience_Normal,
            GravityType.Inverted => P_Ambience_Inverted,
            GravityType.Toggle => P_Ambience_Toggle,
            _ => P_Ambience,
        };
    }

    public ParticleType GetLaunchParticleType()
    {
        if (GravityType == GravityType.None) return P_Launch;

        if (P_Launch_Normal == null)
        {
            const float lightness = 0.5f;
            P_Launch_Normal = new ParticleType(P_Launch)
            {
                Color = GravityType.Normal.Color(),
                Color2 = GravityType.Normal.Color().Lighter(lightness),
            };
            P_Launch_Inverted = new ParticleType(P_Launch)
            {
                Color = GravityType.Inverted.Color(),
                Color2 = GravityType.Inverted.Color().Lighter(lightness),
            };
            P_Launch_Toggle = new ParticleType(P_Launch)
            {
                Color = GravityType.Toggle.Color(),
                Color2 = GravityType.Toggle.Color().Lighter(lightness),
            };
        }

        return GravityType switch
        {
            GravityType.Normal => P_Launch_Normal,
            GravityType.Inverted => P_Launch_Inverted,
            GravityType.Toggle => P_Launch_Toggle,
            _ => P_Launch,
        };
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (IgnoreCoreMode)
            SetFireMode(false);
    }

    public void SetFireMode(bool newFireMode)
    {
        fireMode = newFireMode;
        spriteEvil.Visible = newFireMode;
        sprite.Visible = !newFireMode;
    }

    public override void Update()
    {
        base.Update();

        if (_rippleSprite != null)
        {
            const float ripple_offset = 8f;
            var currentGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;

            if (GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && currentGravity == GravityType.Normal)
            {
                _rippleSprite.Y = -ripple_offset;
                _rippleSprite.Scale.Y = 1f;
            }
            else if (GravityType == GravityType.Normal || GravityType == GravityType.Toggle && currentGravity == GravityType.Inverted)
            {
                _rippleSprite.Y = ripple_offset;
                _rippleSprite.Scale.Y = -1f;
            }
        }
    }
}
