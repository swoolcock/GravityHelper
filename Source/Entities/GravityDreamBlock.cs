// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable RedundantArgumentDefaultValue

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/GravityDreamBlock")]
[TrackedAs(typeof(DreamBlock))]
public class GravityDreamBlock : DreamBlock
{
    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    private readonly FallingComponent _fallingComponent;

    private Vector2 _shakeOffset;

    public GravityType GravityType { get; }
    public Color? LineColor { get; }
    public Color? BackColor { get; }
    public Color? ParticleColor { get; }
    internal bool WasEntered;

    public GravityDreamBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        var fallType = (FallingComponent.FallingType)data.Int("fallType", (int)FallingComponent.FallingType.None);
        if (data.Bool("fall", false)) fallType = data.Bool("fallUp", false) ? FallingComponent.FallingType.Up : FallingComponent.FallingType.Down;

        if (fallType != FallingComponent.FallingType.None)
        {
            Add(_fallingComponent = new FallingComponent
            {
                ClimbFall = data.Bool("climbFall", true),
                FallType = fallType,
                EndOnSolidTiles = data.Bool("endFallOnSolidTiles", true),
                InvertFallingDirFlag = data.Attr("invertFallingDirFlag", ""),
            });
        }

        GravityType = (GravityType)data.Int("gravityType");
        var lineColorString = data.Attr("lineColor");
        var backColorString = data.Attr("backColor");
        var particleColorString = data.Attr("particleColor");
        if (!string.IsNullOrWhiteSpace(lineColorString)) LineColor = Calc.HexToColor(lineColorString);
        if (!string.IsNullOrWhiteSpace(backColorString)) BackColor = Calc.HexToColor(backColorString);
        if (!string.IsNullOrWhiteSpace(particleColorString)) ParticleColor = Calc.HexToColor(particleColorString);

        var textures = particleTextures;
        var prefix = GravityType switch
        {
            GravityType.Normal => "down",
            GravityType.Inverted => "up",
            _ => "double",
        };

        textures[0] = GFX.Game[$"objects/GravityHelper/gravityDreamBlock/{prefix}Arrow"];
        textures[1] = GFX.Game[$"objects/GravityHelper/gravityDreamBlock/{prefix}ArrowSmall"];

        // bring gravity dream blocks above regular dream blocks at the same depth
        Depth--;
    }

    public void InitialiseParticleColors()
    {
        using var _ = new PushRandomDisposable(Scene);
        var baseColor = ParticleColor ?? GravityType.Color();
        if (particles is Array particlesArray)
        {
            for (int i = 0; i < particlesArray.Length; i++)
            {
                var particle = (DreamParticle)particlesArray.GetValue(i);
                var lightness = -0.25f + Calc.Random.NextFloat();
                particle.Color = baseColor.Lighter(lightness);
                particlesArray.SetValue(particle, i);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (WasEntered && (Scene?.Tracker.GetEntity<Player>() is not { } player || player.StateMachine.State != Player.StDreamDash))
            WasEntered = false;
    }

    public void PlayerEntered()
    {
        WasEntered = true;
        GravityHelperModule.PlayerComponent.PreDreamBlockGravityType = GravityHelperModule.PlayerComponent.CurrentGravity;
        GravityHelperModule.PlayerComponent.SetGravity(GravityType);
    }

    public override void Render()
    {
        Position += _shakeOffset;
        base.Render();
        Position -= _shakeOffset;
    }

    public override void OnShake(Vector2 amount) => _shakeOffset += amount;

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        if (_fallingComponent != null)
            _fallingComponent.Triggered = true;
    }
}
