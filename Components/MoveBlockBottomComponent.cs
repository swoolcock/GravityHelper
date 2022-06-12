// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Components
{
    public class MoveBlockBottomComponent : Component
    {
        public readonly List<Image> Images = new();
        public bool BottomPressed;

        private DynData<MoveBlock> _moveBlockData;

        public MoveBlockBottomComponent() : base(true, false)
        {
        }

        public override void Added(Entity entity)
        {
            base.Added(entity);

            if (entity is not MoveBlock moveBlock) return;

            _moveBlockData = new DynData<MoveBlock>(moveBlock);

            // add button
            MTexture buttonTexture = GFX.Game["objects/moveBlock/button"];
            int numTiles = (int) (moveBlock.Width / 8);
            for (int index = 0; index < numTiles; ++index)
            {
                int tileX = index == 0 ? 0 : (index < numTiles - 1 ? 1 : 2);
                moveBlock.CallAddImage(
                    buttonTexture.GetSubtexture(tileX * 8, 0, 8, 8),
                    new Vector2(index * 8f, moveBlock.Height + 4f),
                    0f,
                    new Vector2(1, -1),
                    Images);
            }

            // replace bottom tiles with inverse top tiles
            var bodyTiles = _moveBlockData.Get<List<Image>>("body");
            var topTiles = bodyTiles.Where(i => i.Y <= 8).OrderBy(i => i.X).ToArray();
            var bottomTiles = bodyTiles.Where(i => i.Y >= moveBlock.Height - 8).OrderBy(i => i.X).ToArray();

            for (int i = 0; i < topTiles.Length; i++)
            {
                bottomTiles[i].Texture = topTiles[i].Texture;
                bottomTiles[i].FlipY = true;
            }

            foreach (var image in Images)
                image.Y = Entity.Height;
        }

        public override void Update()
        {
            var collidePlayer = Entity.CollideCheck<Player>(Entity.Position + Vector2.UnitY);

            foreach (var image in Images)
                image.Y = Entity.Height + (collidePlayer ? -2f : 0f);

            if (collidePlayer && !BottomPressed)
                Audio.Play("event:/game/04_cliffside/arrowblock_side_depress", Entity.Position);

            if (!collidePlayer && BottomPressed)
                Audio.Play("event:/game/04_cliffside/arrowblock_side_release", Entity.Position);

            BottomPressed = collidePlayer;
        }

        public override void Render()
        {
            var fillColor = _moveBlockData.Get<Color>("fillColor");
            foreach (var image in Images)
            {
                image.Color = fillColor;
                image.Render();
            }
        }
    }
}
