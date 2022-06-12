// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
