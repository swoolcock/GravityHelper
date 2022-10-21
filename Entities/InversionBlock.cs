// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [Tracked]
    [CustomEntity("GravityHelper/InversionBlock")]
    public class InversionBlock : Solid
    {
        private readonly Color _fillColor = Calc.HexToColor("0b1013");
        private const int tile_size = 8;

        public Edges Edges { get; }
        public GravityType LeftGravityType { get; }
        public GravityType RightGravityType { get; }

        public InversionBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
            var widthInTiles = (int)Width / tile_size;
            var heightInTiles = (int)Height / tile_size;
            var texture = GFX.Game[$"objects/GravityHelper/inversionBlock/idle_00"];

            LeftGravityType = data.Enum("leftGravityType", GravityType.Toggle);
            RightGravityType = data.Enum("rightGravityType", GravityType.Toggle);

            Edges |= data.Bool("leftEnabled") ? Edges.Left : Edges.None;
            Edges |= data.Bool("rightEnabled") ? Edges.Right : Edges.None;
            Edges |= data.Bool("topEnabled", true) ? Edges.Top : Edges.None;
            Edges |= data.Bool("bottomEnabled", true) ? Edges.Bottom : Edges.None;

            // centre
            // addImage(texture.GetSubtexture(tile_size, tile_size, tile_size, tile_size), new Vector2(tile_size), new Vector2(widthInTiles - 2, heightInTiles - 2));
            // top left
            addImage(texture.GetSubtexture(0, 0, tile_size, tile_size), Vector2.Zero);
            // top right
            addImage(texture.GetSubtexture(2 * tile_size, 0, tile_size, tile_size), new Vector2(Width - tile_size, 0));
            // bottom left
            addImage(texture.GetSubtexture(0, 2 * tile_size, tile_size, tile_size), new Vector2(0, Height - tile_size));
            // bottom right
            addImage(texture.GetSubtexture(2 * tile_size, 2 * tile_size, tile_size, tile_size), new Vector2(Width - tile_size, Height - tile_size));

            for (int x = 1; x < widthInTiles - 1; x++)
            {
                // top
                addImage(texture.GetSubtexture(tile_size, 0, tile_size, tile_size), new Vector2(x * tile_size, 0));
                // bottom
                addImage(texture.GetSubtexture(tile_size, 2 * tile_size, tile_size, tile_size), new Vector2(x * tile_size, Height - tile_size));
            }

            for (int y = 1; y < heightInTiles - 1; y++)
            {
                // left
                addImage(texture.GetSubtexture(0, tile_size, tile_size, tile_size), new Vector2(0, y * tile_size));
                // right
                addImage(texture.GetSubtexture(2 * tile_size, tile_size, tile_size, tile_size), new Vector2(Width - tile_size, y * tile_size));
            }
        }

        private Image addImage(MTexture subTexture, Vector2 position, Vector2? scale = null, float rotation = 0f)
        {
            var image = new Image(subTexture)
            {
                Position = position + new Vector2(4f),
                Rotation = rotation,
                Scale = scale ?? Vector2.One,
            }.CenterOrigin();
            Add(image);
            return image;
        }

        public override void Render()
        {
            base.Render();
            Draw.Rect(X + tile_size, Y + tile_size, Width - 2 * tile_size, Height - 2 * tile_size, _fillColor);
        }

        public bool TryHandlePlayer(Player player)
        {
            if (!HasPlayerRider()) return false;

            if (player.Top < Top && player.StateMachine.State != Player.StClimb)
            {
                // only warp the player if gravity flip succeeded
                if (player.SetGravity(GravityType.Inverted, 0f))
                    player.Top = Bottom;
            }
            else if (player.Bottom > Bottom && player.StateMachine.State != Player.StClimb)
            {
                // only warp the player if gravity flip succeeded
                if (player.SetGravity(GravityType.Normal, 0f))
                    player.Bottom = Top;
            }
            else if (player.Left < Left && player.StateMachine.State == Player.StClimb)
            {
                player.SetGravity(GravityType.Toggle, 0f);
                player.Left = Right;
                if (Input.MoveX <= 0) player.Facing = (Facings)(-(int)player.Facing);
            }
            else if (player.Right > Right && player.StateMachine.State == Player.StClimb)
            {
                player.SetGravity(GravityType.Toggle, 0f);
                player.Right = Left;
                if (Input.MoveX >= 0) player.Facing = (Facings)(-(int)player.Facing);
            }

            return true;
        }
    }
}
