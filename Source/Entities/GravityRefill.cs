// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/GravityRefill = CreateRefill",
    "GravityHelper/GravityRefillWall = CreateWall")]
public class GravityRefill : Entity
{
    // properties
    public bool OneUse { get; }
    public int Charges { get; }
    public int Dashes { get; }
    public bool RefillsDash { get; }
    public bool RefillsStamina { get; }
    public float RespawnTime { get; }

    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    // components
    private Sprite _sprite;
    private Sprite _arrows;
    private Image _outline;
    private Wiggler _wiggler;
    private BloomPoint _bloom;
    private VertexLight _light;
    private SineWave _sine;

    // particles
    public static readonly ParticleType P_Shatter = new ParticleType(Refill.P_Shatter)
    {
        Color = Color.Purple,
        Color2 = Color.MediumPurple,
    };

    public static readonly ParticleType P_Regen = new ParticleType(Refill.P_Regen)
    {
        Color = Color.BlueViolet,
        Color2 = Color.Violet,
    };

    public static readonly ParticleType P_Glow_Normal = new ParticleType(Refill.P_Glow)
    {
        Color = Color.Blue,
        Color2 = Color.BlueViolet,
    };

    public static readonly ParticleType P_Glow_Inverted = new ParticleType(Refill.P_Glow)
    {
        Color = Color.Red,
        Color2 = Color.MediumVioletRed,
    };

    private Level _level;
    private float _respawnTimeRemaining;
    private float _arrowIntervalOffset;

    private bool _emitNormal;
    private bool _isWall;
    private float _wallAlpha = 0.8f;
    private WallRenderMode _wallRenderMode;

    // if set to true, dashing over the top of a gravity refill
    // while Madeline already has a charge will not refund it
    private bool _legacyRefillBehavior;

    internal GravityRefill(Vector2 position, bool twoDashes, bool oneUse) : base(position)
    {
        Charges = twoDashes ? 2 : 1;
        Dashes = twoDashes ? 2 : -1;
        OneUse = oneUse;
        RefillsDash = true;
        RefillsStamina = true;
        RespawnTime = 2.5f;

        init((int)(position.X + position.Y));
    }

    [UsedImplicitly]
    public static Entity CreateRefill(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravityRefill(entityData, offset, false);

    [UsedImplicitly]
    public static Entity CreateWall(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new GravityRefill(entityData, offset, true);

    public GravityRefill(EntityData data, Vector2 offset, bool wall)
        : base(data.Position + offset)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        _isWall = wall;

        Charges = data.Int("charges", 1);
        Dashes = data.Int("dashes", -1);
        OneUse = data.Bool("oneUse");
        RefillsDash = data.Bool("refillsDash", true);
        RefillsStamina = data.Bool("refillsStamina", true);
        RespawnTime = data.Float("respawnTime", 2.5f);
        _wallAlpha = data.Float("wallAlpha", 0.8f);
        _legacyRefillBehavior = data.Bool("legacyRefillBehavior", true);

        init(data.ID, data.Width, data.Height);
    }

    private void init(int randomSeed, int width = 0, int height = 0)
    {
        Collider = _isWall ? new Hitbox(width, height) : new Hitbox(16f, 16f, -8f, -8f);
        Depth = _isWall ? Depths.TheoCrystal : Depths.Pickups;

        var path = "objects/GravityHelper/gravityRefill";
        var outlineName = Dashes == 2 ? "outline_two_dash" : "outline";
        var animationName = !RefillsDash ? "idle_no_dash" : Dashes == 2 ? "idle_two_dash" : "idle";

        // add components
        Add(new PlayerCollider(OnPlayer),
            _sprite = GFX.SpriteBank.Create("gravityRefill"),
            _arrows = GFX.SpriteBank.Create("gravityRefillArrows"),
            _wiggler = Wiggler.Create(1f, 4f, v => _sprite.Scale = Vector2.One * (float)(1.0 + v * 0.2)),
            new MirrorReflection());

        if (!_isWall)
        {
            Add(_bloom = new BloomPoint(0.8f, 16f),
                _light = new VertexLight(Color.White, 1f, 16, 48),
                _sine = new SineWave(0.6f, 0.0f),
                _outline = new Image(GFX.Game[$"{path}/{outlineName}"]) { Visible = false });

            _outline.CenterOrigin();
        }

        // this uses a random sample but so as to not break existing maps i'm leaving it above the PushRandomDisposable
        _sprite.Play(animationName, true, true);

        using var _ = new PushRandomDisposable(randomSeed);
        _sine?.Randomize();
        _arrows.OnFinish = _ => _arrows.Visible = false;
        _arrowIntervalOffset = Calc.Random.NextFloat(2f);

        updateSpritePos();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        _level = SceneAs<Level>();
    }

    public override void Update()
    {
        base.Update();

        if (_respawnTimeRemaining > 0.0)
        {
            _respawnTimeRemaining -= Engine.DeltaTime;
            if (_respawnTimeRemaining <= 0.0)
                respawn();
        }
        else if (Scene.OnInterval(0.1f) && !_isWall)
        {
            var offset = Vector2.UnitY * (_emitNormal ? 5f : -5f);
            var range = Vector2.One * 4f;
            var direction = Vector2.UnitY.Angle() * (_emitNormal ? 1 : -1);
            var p_glow = _emitNormal ? P_Glow_Normal : P_Glow_Inverted;
            _level.ParticlesFG.Emit(p_glow, 1, Position + offset, range, direction);
            _emitNormal = !_emitNormal;
        }

        updateSpritePos();

        if (_light != null && _bloom != null)
        {
            _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0.0f, 4f * Engine.DeltaTime);
            _bloom.Alpha = _light.Alpha * 0.8f;
        }

        if (Scene.OnInterval(2f, _arrowIntervalOffset) && _sprite.Visible && (!_isWall || _wallRenderMode == WallRenderMode.Filled))
        {
            var arrowName = Dashes == 2 ? "arrows_two_dash" : "arrows";
            _arrows.Play(arrowName, true);
            _arrows.Visible = true;
        }
    }

    private void respawn()
    {
        if (Collidable) return;
        Collidable = true;

        _wallRenderMode = WallRenderMode.Filled;

        _sprite.Visible = true;
        _arrows.Stop();

        if (_outline != null)
            _outline.Visible = false;

        Depth = _isWall ? Depths.TheoCrystal : Depths.Pickups;

        _wiggler?.Start();
        Audio.Play("event:/game/general/diamond_return", Center);

        _level.ParticlesFG.Emit(P_Regen, 16, Center, Vector2.One * 2f);
    }

    private void updateSpritePos()
    {
        if (_isWall)
            _arrows.Position = _sprite.Position = Collider.Center;
        else
            _arrows.Y = _sprite.Y = _bloom.Y = _sine.Value * 2f;
    }

    public override void Render()
    {
        if (!_isWall)
        {
            if (_sprite.Visible)
                _sprite.DrawOutline();
            base.Render();
            return;
        }

        Camera camera = SceneAs<Level>().Camera;
        if (Right < (double)camera.Left || Left > (double)camera.Right || Bottom < (double)camera.Top ||
            Top > (double)camera.Bottom)
            return;

        getColors(out var fillColor, out var borderColor);

        // make sure the sprites are opaque
        _arrows.Color = _sprite.Color = Color.White;

        switch (_wallRenderMode)
        {
            case WallRenderMode.Flash when !OneUse:
            case WallRenderMode.Filled when !OneUse:
                // draw outline 1 pixel out
                Draw.HollowRect(X - 1f, Y - 1f, Width + 2f, Height + 2f, borderColor);

                // draw filled rectangle 1 pixel in
                Draw.Rect(X + 1f, Y + 1f, Width - 2f, Height - 2f, fillColor);

                break;

            case WallRenderMode.Flash when OneUse:
            case WallRenderMode.Filled when OneUse:
                // draw a translucent outline 1 pixel out
                Draw.HollowRect(X - 1f, Y - 1f, Width + 2f, Height + 2f, borderColor * 0.6f);

                // draw horizontal segments 1 pixel out
                for (int index = 0; index < (double)Width; index += 8)
                {
                    Draw.Line(TopLeft - Vector2.UnitY + Vector2.UnitX * (index + 2), TopLeft - Vector2.UnitY + Vector2.UnitX * (index + 6), borderColor);
                    Draw.Line(BottomLeft + Vector2.UnitX * (index + 2), BottomLeft + Vector2.UnitX * (index + 6), borderColor);
                }

                // draw vertical segments 1 pixel out
                for (int index = 0; index < (double)Height; index += 8)
                {
                    Draw.Line(TopLeft + Vector2.UnitY * (index + 2), TopLeft + Vector2.UnitY * (index + 6), borderColor);
                    Draw.Line(TopRight + Vector2.UnitX + Vector2.UnitY * (index + 2), TopRight + Vector2.UnitX + Vector2.UnitY * (index + 6), borderColor);
                }

                // draw filled rectangle 1 pixel in
                Draw.Rect(X + 1f, Y + 1f, Width - 2f, Height - 2f, fillColor);

                break;

            case WallRenderMode.Returning:
                // draw horizontal segments
                for (int index = 0; index < (double)Width; index += 8)
                {
                    Draw.Line(TopLeft + Vector2.UnitX * (index + 2), TopLeft + Vector2.UnitX * (index + 6), borderColor);
                    Draw.Line(BottomLeft - Vector2.UnitY + Vector2.UnitX * (index + 2), BottomLeft - Vector2.UnitY + Vector2.UnitX * (index + 6), borderColor);
                }

                // draw vertical segments
                for (int index = 0; index < (double)Height; index += 8)
                {
                    Draw.Line(TopLeft + Vector2.UnitX + Vector2.UnitY * (index + 2), TopLeft + Vector2.UnitX + Vector2.UnitY * (index + 6), borderColor);
                    Draw.Line(TopRight + Vector2.UnitY * (index + 2), TopRight + Vector2.UnitY * (index + 6), borderColor);
                }

                // set alpha on the sprites
                _arrows.Color = _sprite.Color = Color.White * 0.25f;

                break;
        }

        base.Render();
    }

    private void OnPlayer(Player player)
    {
        if (player.Get<PlayerGravityComponent>() is not { } playerGravityComponent) return;

        var dashes = Dashes < 0 ? player.MaxDashes : Dashes;
        bool canUse = RefillsDash && player.Dashes < dashes ||
                      RefillsStamina && player.Stamina < 20 ||
                      playerGravityComponent.GravityCharges < Charges;

        if (!canUse) return;

        if (RefillsDash && Dashes < 0)
            player.RefillDash();
        else if (RefillsDash && player.Dashes < Dashes)
            player.Dashes = Dashes;

        if (RefillsStamina) player.RefillStamina();
        playerGravityComponent.RefillGravityCharges(Charges, !_legacyRefillBehavior);

        Audio.Play("event:/game/general/diamond_touch", Position);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        Collidable = false;
        Add(new Coroutine(refillRoutine(player)));
        _respawnTimeRemaining = RespawnTime;
        _wallRenderMode = WallRenderMode.Flash;
    }

    private IEnumerator refillRoutine(Player player)
    {
        GravityRefill refill = this;
        Celeste.Freeze(0.05f);
        yield return null;

        _wallRenderMode = WallRenderMode.Returning;
        refill._level.Shake();
        refill._arrows.Visible = false;
        refill._sprite.Visible = _isWall;
        if (!refill.OneUse && refill._outline != null)
            refill._outline.Visible = true;
        refill.Depth = Depths.BGDecals - 1;
        yield return 0.05f;

        if (!_isWall)
        {
            float direction = player.Speed.Angle();
            refill._level.ParticlesFG.Emit(P_Shatter, 5, refill.Position, Vector2.One * 4f, direction - (float)Math.PI / 2f);
            refill._level.ParticlesFG.Emit(P_Shatter, 5, refill.Position, Vector2.One * 4f, direction + (float)Math.PI / 2f);
            SlashFx.Burst(refill.Position, direction);
        }

        if (refill.OneUse)
            refill.RemoveSelf();
    }

    private void getColors(out Color fillColor, out Color borderColor)
    {
        switch (_wallRenderMode)
        {
            case WallRenderMode.Filled:
                if (Dashes == 2)
                {
                    borderColor = Color.Orchid * _wallAlpha;
                    fillColor = Color.DarkOrchid * _wallAlpha;
                }
                else if (!RefillsDash)
                {
                    borderColor = Color.LightSkyBlue * _wallAlpha;
                    fillColor = Color.CornflowerBlue * _wallAlpha;
                }
                else
                {
                    borderColor = Color.BlueViolet * _wallAlpha;
                    fillColor = Color.Indigo * _wallAlpha;
                }

                break;

            case WallRenderMode.Flash:
                fillColor = borderColor = Color.White * _wallAlpha;
                break;

            case WallRenderMode.Returning:
                borderColor = Color.Gray * (_wallAlpha * 0.25f);
                fillColor = Color.Transparent;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum WallRenderMode
    {
        Filled,
        Flash,
        Returning,
    }
}
