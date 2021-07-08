// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/UpsideDownJumpThru")]
    [Tracked]
    public class UpsideDownJumpThru : JumpThru
    {
        private readonly int columns;
        private readonly string overrideTexture;
        private readonly int overrideSoundIndex;

        public UpsideDownJumpThru(Vector2 position, int width, string overrideTexture, int overrideSoundIndex = -1)
            : base(position, width, true)
        {
            columns = width / 8;
            Depth = -60;
            this.overrideTexture = overrideTexture;
            this.overrideSoundIndex = overrideSoundIndex;

            Collider.Top = 3;
        }

        public UpsideDownJumpThru(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width,
                data.Attr("texture", "default"),
                data.Int("surfaceIndex", -1))
        {
        }

        public override void Awake(Scene scene)
        {
            EnsureCorrectTracking(scene);

            string str = AreaData.Get(scene).Jumpthru;
            if (!string.IsNullOrEmpty(overrideTexture) && !overrideTexture.Equals("default"))
                str = overrideTexture;

            SurfaceSoundIndex = overrideSoundIndex > 0
                ? overrideSoundIndex
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
            for (int i = 0; i < columns; ++i)
            {
                int xOffset;
                int yOffset;
                if (i == 0)
                {
                    xOffset = 0;
                    yOffset = CollideCheck<Solid, SwapBlock, ExitBlock>(Position + new Vector2(-1f, 0.0f)) ? 0 : 1;
                }
                else if (i == columns - 1)
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
