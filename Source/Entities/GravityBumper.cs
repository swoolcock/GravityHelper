// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/GravityBumper")]
public class GravityBumper : Bumper
{
    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    public GravityType GravityType { get; }
    public bool IgnoreCoreMode { get; }
    public bool SingleUse { get; }
    public bool Static { get; }

    private readonly Sprite _rippleSprite;
    private readonly bool _randomizeFrame;
    internal readonly float _respawnTime;
    private readonly string _spriteName;
    private readonly string _evilSpriteName;

    public GravityBumper(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        using var _ = new PushRandomDisposable(data.ID);

        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        GravityType = (GravityType)data.Int("gravityType");
        IgnoreCoreMode = data.Bool("ignoreCoreMode");
        SingleUse = data.Bool("singleUse");

        if (IgnoreCoreMode && Get<CoreModeListener>() is { } coreModeListener)
            Remove(coreModeListener);

        sine.Rate = data.Float("wobbleRate", 1f);
        Static = data.Bool("static", false);
        _randomizeFrame = data.Bool("randomizeFrame", true);
        _respawnTime = data.Float("respawnTime", 0.6f);
        _spriteName = data.Attr("spriteName");
        _evilSpriteName = data.Attr("evilSpriteName");

        if (_respawnTime <= 0)
            _respawnTime = float.MaxValue / 2f;

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

        // update the position manually since the sine may have changed since base()
        UpdatePosition();

        var spriteName = _spriteName;
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            spriteName = GravityType switch
            {
                GravityType.Normal => "gravityBumperNormal",
                GravityType.Inverted => "gravityBumperInvert",
                GravityType.Toggle => "gravityBumperToggle",
                _ => "",
            };
        }

        if (!string.IsNullOrWhiteSpace(spriteName))
            GFX.SpriteBank.CreateOn(sprite, spriteName);

        if (!string.IsNullOrWhiteSpace(_evilSpriteName))
            GFX.SpriteBank.CreateOn(spriteEvil, _evilSpriteName);

        if (GravityType != GravityType.None)
        {
            Add(_rippleSprite = GFX.SpriteBank.Create("gravityRipple"));
            _rippleSprite.Color = GravityType.HighlightColor();
            _rippleSprite.Play("loop");
        }

        if (_randomizeFrame)
            sprite.Play("idle", true, true);

        if (Get<PlayerCollider>() is { } playerCollider)
            playerCollider.OnCollide = OnPlayer;

        Add(new AccessibilityListener(onAccessibilityChange));
        onAccessibilityChange();
    }

    private void onAccessibilityChange()
    {
        if (_rippleSprite != null)
            _rippleSprite.Color = GravityType.RippleColor();
    }

    private new void OnPlayer(Player player)
    {
        if (fireMode)
        {
            if (SaveData.Instance.Assists.Invincible) return;

            Vector2 hitDirection = (player.Center - Center).SafeNormalize();
            hitDir = -hitDirection;
            hitWiggler.Start();
            Audio.Play(SFX.game_09_hotpinball_activate, Position);
            respawnTimer = _respawnTime;
            player.Die(hitDirection);
            SceneAs<Level>().Particles.Emit(P_FireHit, 12, Center + hitDirection * 12f, Vector2.One * 3f, hitDirection.Angle());
        }
        else
        {
            if (respawnTimer > 0.0f) return;
            respawnTimer = _respawnTime;

            Audio.Play(SceneAs<Level>().Session.Area.ID == 9 ? SFX.game_09_pinballbumper_hit : SFX.game_06_pinballbumper_hit, Position);

            // change gravity first if required
            GravityHelperModule.PlayerComponent?.SetGravity(GravityType);

            // now launch
            Vector2 launchDirection = player.ExplodeLaunch(Position, false, false);

            // set to fire mode if it's single use
            if (SingleUse)
                SetFireMode(true);

            // update sprites and light/bloom
            sprite.Play("hit", true);
            spriteEvil.Play("hit", true);
            light.Visible = false;
            bloom.Visible = false;

            // effects
            var colorScheme = GravityHelperModule.Settings.GetColorScheme();
            SceneAs<Level>().DirectionalShake(launchDirection, 0.15f);
            SceneAs<Level>().Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f);
            SceneAs<Level>().Particles.Emit(colorScheme.GetBumperLaunchParticleType(GravityType), 12, Center + launchDirection * 12f, Vector2.One * 3f, launchDirection.Angle());
        }
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

            _rippleSprite.Visible = respawnTimer <= 0 && !fireMode;
        }
    }

    public override void Render()
    {
        var scheme = GravityHelperModule.Settings.GetColorScheme();

        if (!spriteEvil.Visible && GravityType != GravityType.None && scheme.NeedsShader)
        {
            using (GravityHelperAPI.Exports.WithCustomTintShader())
            {
                if (_rippleSprite != null) _rippleSprite.Visible = false;
                sprite.Color = GravityType.Color(scheme);

                base.Render();

                sprite.Color = Color.White;
                if (_rippleSprite != null) _rippleSprite.Visible = true;
            }

            _rippleSprite?.Render();
        }
        else
            base.Render();
    }
}
