// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/UpsideDownJumpThru")]
[Tracked]
public class UpsideDownJumpThru : JumpThru
{
    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    private readonly int _columns;
    private readonly string _overrideTexture;
    private readonly int _overrideSoundIndex;
    private readonly bool _attached;
    private readonly bool _triggerStaticMovers;
    private readonly bool _invisible;

    private Vector2 _shakeOffset;
    private Platform _attachedPlatform;
    private StaticMover _staticMover;

    public UpsideDownJumpThru(Vector2 position, int width, string overrideTexture, int overrideSoundIndex = -1,
        bool safe = true, bool attached = false, bool triggerStaticMovers = true, bool invisible = false)
        : base(position, width, safe)
    {
        _modVersion = default;
        _pluginVersion = default;

        _columns = width / 8;
        _overrideTexture = overrideTexture;
        _overrideSoundIndex = overrideSoundIndex;
        _attached = attached;
        _triggerStaticMovers = triggerStaticMovers;
        _invisible = invisible;

        init();
    }

    public UpsideDownJumpThru(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Width, true)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        _columns = data.Width / 8;
        _overrideTexture = data.Attr("texture", "default");
        _overrideSoundIndex = data.Int("surfaceIndex", -1);
        _attached = data.Bool("attached", false);
        _triggerStaticMovers = data.Bool("triggerStaticMovers", true);
        _invisible = data.Bool("invisible", false);

        init();
    }

    private void init()
    {
        if (_attached)
        {
            List<Actor> sharedRiders = new();
            Add(_staticMover = new StaticMover
            {
                SolidChecker = solid =>
                    solid is not FloatySpaceBlock && // moon blocks handle attached jumpthrus automatically
                    (CollideCheck(solid, Position - Vector2.UnitX) || CollideCheck(solid, Position + Vector2.UnitX)),
                OnMove = amount =>
                {
                    sharedRiders.Clear();
                    // get all the actors that are riding both the jumpthru and the attached solid, and make them ignore jumpthrus
                    if (_attachedPlatform is Solid solid)
                    {
                        foreach (Actor actor in Scene?.Tracker.GetEntities<Actor>())
                        {
                            if (actor.IsRiding(this) && actor.IsRiding(solid) && !actor.IgnoreJumpThrus && !actor.TreatNaive)
                            {
                                actor.IgnoreJumpThrus = true;
                                actor.TreatNaive = true;
                                sharedRiders.Add(actor);
                            }
                        }
                    }

                    // move the jumpthru and any riders that aren't also riding the attached solid
                    MoveH(amount.X);
                    MoveV(amount.Y);

                    // reset ignore jumpthrus if we must
                    foreach (var rider in sharedRiders)
                    {
                        rider.IgnoreJumpThrus = false;
                        rider.TreatNaive = false;
                    }
                    sharedRiders.Clear();
                },
                OnShake = amount =>
                {
                    ShakeStaticMovers(amount);
                    _shakeOffset += amount;
                },
                OnAttach = p =>
                {
                    _attachedPlatform = p;
                    Depth = p.Depth + 1;
                },
                OnEnable = () =>
                {
                    EnableStaticMovers();
                    Active = Visible = Collidable = true;
                },
                OnDisable = () =>
                {
                    DisableStaticMovers();
                    Active = Visible = Collidable = false;
                }
            });
        }

        Depth = -60;
        Collider.Top = 3;
    }

    public override void Render()
    {
        Position += _shakeOffset;
        base.Render();
        Position -= _shakeOffset;
    }

    public override void Update()
    {
        base.Update();

        if (_attachedPlatform != null && _attached && _triggerStaticMovers && HasPlayerRider())
            triggerPlatform();
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        _attachedPlatform = null;
    }

    public override void Awake(Scene scene)
    {
        string str = AreaData.Get(scene).Jumpthru;
        if (!string.IsNullOrEmpty(_overrideTexture) && !_overrideTexture.Equals("default"))
            str = _overrideTexture;

        SurfaceSoundIndex = _overrideSoundIndex > 0
            ? _overrideSoundIndex
            : str.ToLower() switch
            {
                "dream" => 32,
                "temple" => 8,
                "templeb" => 8,
                "core" => 3,
                _ => 5,
            };

        using var _ = new PushRandomDisposable(scene);

        // "invisible" determines whether we add image components to the entity
        // this allows us to leave Visible = true so that subclasses can inherit the shake offset functionality
        if (!_invisible)
        {
            var mtexture = GFX.Game[$"objects/jumpthru/{str}"];
            int textureWidthInTiles = mtexture.Width / 8;
            for (int i = 0; i < _columns; ++i)
            {
                int xOffset;
                int yOffset;
                if (i == 0)
                {
                    xOffset = 0;
                    yOffset = CollideCheck<Solid, SwapBlock, ExitBlock>(Position + new Vector2(-1f, 0.0f)) ? 0 : 1;
                }
                else if (i == _columns - 1)
                {
                    xOffset = textureWidthInTiles - 1;
                    yOffset = CollideCheck<Solid, SwapBlock, ExitBlock>(Position + new Vector2(1f, 0.0f)) ? 0 : 1;
                }
                else
                {
                    xOffset = 1 + Calc.Random.Next(textureWidthInTiles - 2);
                    yOffset = Calc.Random.Choose(0, 1);
                }

                Add(new Image(mtexture.GetSubtexture(xOffset * 8, yOffset * 8, 8, 8))
                {
                    X = i * 8,
                    Y = 8,
                    Scale = {Y = -1},
                });
            }
        }

        foreach (StaticMover mover in scene.Tracker.GetComponents<StaticMover>())
        {
            if (mover.IsRiding(this) && mover.Platform == null)
            {
                staticMovers.Add(mover);
                mover.Platform = this;
                mover.OnAttach?.Invoke(this);
            }
        }
    }


    public override void OnStaticMoverTrigger(StaticMover sm) => triggerPlatform();

    private void triggerPlatform() => _attachedPlatform?.OnStaticMoverTrigger(_staticMover);
}
