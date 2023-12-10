// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
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

        private Vector2 shakeOffset;
        private Platform _attachedPlatform;
        private readonly StaticMover _staticMover;

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
                                if (actor.IsRiding(this) && actor.IsRiding(solid) && !actor.IgnoreJumpThrus)
                                {
                                    actor.IgnoreJumpThrus = true;
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
                        }
                        sharedRiders.Clear();
                    },
                    OnShake = amount => shakeOffset += amount,
                    OnAttach = p => _attachedPlatform = p,
                });
            }

            Depth = -60;
            Collider.Top = 3;
        }

        public override void Render()
        {
            Position += shakeOffset;
            base.Render();
            Position -= shakeOffset;
        }

        public override void Update()
        {
            base.Update();

            if (_attachedPlatform != null && _attached && _triggerStaticMovers && HasPlayerRider())
            {
                _attachedPlatform.OnStaticMoverTrigger(_staticMover);
            }
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
    }
}
