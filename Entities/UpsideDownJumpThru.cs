// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly int _columns;
        private readonly string _overrideTexture;
        private readonly int _overrideSoundIndex;

        private Vector2 _shakeOffset;

        public UpsideDownJumpThru(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, true)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            _columns = data.Width / 8;
            _overrideTexture = data.Attr("texture", "default");
            _overrideSoundIndex = data.Int("surfaceIndex", -1);

            Depth = -60;
            Collider.Top = 3;

            Add(new StaticMover
            {
                SolidChecker = s => CollideCheck(s, Position - Vector2.UnitX) || CollideCheck(s, Position + Vector2.UnitX),
                OnMove = v =>
                {
                    MoveH(v.X);
                    MoveV(v.Y);
                },
                OnShake = v => _shakeOffset += v,
            });
        }

        public override void Awake(Scene scene)
        {
            EnsureCorrectTracking(scene);

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

        public override void Render()
        {
            Position += _shakeOffset;
            base.Render();
            Position -= _shakeOffset;
        }

        public void EnsureCorrectTracking(Scene scene = null)
        {
            scene ??= Scene;
            if (scene == null) return;

            // ensure we're only tracked as UpsideDownJumpThru
            if (scene.Tracker.Entities.TryGetValue(typeof(JumpThru), out var jumpthrus) && jumpthrus.Contains(this))
                jumpthrus.Remove(this);
            if (!scene.Tracker.Entities.TryGetValue(typeof(UpsideDownJumpThru), out var upsidedown))
                upsidedown = scene.Tracker.Entities[typeof(UpsideDownJumpThru)] = new List<Entity>();
            if (!upsidedown.Contains(this))
                upsidedown.Add(this);
        }
    }
}
