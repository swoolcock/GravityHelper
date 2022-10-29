// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

        private readonly MTexture _blockTexture;
        private readonly MTexture _edgeTexture;
        private readonly MTexture _normalEdgeTexture;
        private readonly MTexture _invertedEdgeTexture;
        private readonly MTexture _toggleEdgeTexture;


        public InversionBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, true)
        {
            _blockTexture = GFX.Game[$"objects/GravityHelper/inversionBlock/block"];
            _edgeTexture = GFX.Game[$"objects/GravityHelper/inversionBlock/edges"];
            _normalEdgeTexture = _edgeTexture.GetSubtexture(0, 0, 3 * tile_size, tile_size);
            _invertedEdgeTexture = _edgeTexture.GetSubtexture(0, tile_size, 3 * tile_size, tile_size);
            _toggleEdgeTexture = _edgeTexture.GetSubtexture(0, 2 * tile_size, 3 * tile_size, tile_size);

            LeftGravityType = data.Enum("leftGravityType", GravityType.Toggle);
            RightGravityType = data.Enum("rightGravityType", GravityType.Toggle);

            Edges |= data.Bool("leftEnabled") ? Edges.Left : Edges.None;
            Edges |= data.Bool("rightEnabled") ? Edges.Right : Edges.None;
            Edges |= data.Bool("topEnabled", true) ? Edges.Top : Edges.None;
            Edges |= data.Bool("bottomEnabled", true) ? Edges.Bottom : Edges.None;
        }

        private MTexture textureForGravityType(GravityType type) => type switch
        {
            GravityType.Normal => _normalEdgeTexture,
            GravityType.Inverted => _invertedEdgeTexture,
            GravityType.Toggle => _toggleEdgeTexture,
            _ => null,
        };

        public override void Render()
        {
            // centre
            Draw.Rect(X + tile_size, Y + tile_size, Width - 2 * tile_size, Height - 2 * tile_size, _fillColor);

            var leftTexture = !Edges.HasFlag(Edges.Left) ? null : textureForGravityType(LeftGravityType);
            var rightTexture = !Edges.HasFlag(Edges.Right) ? null : textureForGravityType(RightGravityType);
            var topTexture = !Edges.HasFlag(Edges.Top) ? null : _normalEdgeTexture;
            var bottomTexture = !Edges.HasFlag(Edges.Bottom) ? null : _invertedEdgeTexture;
            var origin = new Vector2(tile_size / 2f, tile_size / 2f);

            // top left corner
            var position = TopLeft;
            _blockTexture.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(0, 0, tile_size, tile_size));
            leftTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)-Math.PI / 2, new Rectangle(2 * tile_size, 0, tile_size, tile_size));
            topTexture?.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(0, 0, tile_size, tile_size));

            // top right corner
            position = TopRight - Vector2.UnitX * tile_size;
            _blockTexture.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(2 * tile_size, 0, tile_size, tile_size));
            rightTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)Math.PI / 2, new Rectangle(0, 0, tile_size, tile_size));
            topTexture?.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(2 * tile_size, 0, tile_size, tile_size));

            // bottom left corner
            position = BottomLeft - Vector2.UnitY * tile_size;
            _blockTexture.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(0, 2 * tile_size, tile_size, tile_size));
            leftTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)-Math.PI / 2, new Rectangle(0, 0, tile_size, tile_size));
            bottomTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)Math.PI, new Rectangle(2 * tile_size, 0, tile_size, tile_size));

            // bottom right corner
            position = BottomRight - Vector2.One * tile_size;
            _blockTexture.Draw(position, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(2 * tile_size, 2 * tile_size, tile_size, tile_size));
            rightTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)Math.PI / 2, new Rectangle(2 * tile_size, 0, tile_size, tile_size));
            bottomTexture?.Draw(position + origin, origin, Color.White, Vector2.One, (float)Math.PI, new Rectangle(0, 0, tile_size, tile_size));

            // horizontal edges
            for (int y = tile_size; y < Height - tile_size; y += tile_size)
            {
                var leftPos = new Vector2(Left, Top + y);
                var rightPos = new Vector2(Right - tile_size, Top + y);
                _blockTexture.Draw(leftPos, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(0, tile_size, tile_size, tile_size));
                leftTexture?.Draw(leftPos + origin, origin, Color.White, Vector2.One, (float)-Math.PI / 2, new Rectangle(tile_size, 0, tile_size, tile_size));
                _blockTexture.Draw(rightPos, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(2 * tile_size, tile_size, tile_size, tile_size));
                rightTexture?.Draw(rightPos + origin, origin, Color.White, Vector2.One, (float)Math.PI / 2, new Rectangle(tile_size, 0, tile_size, tile_size));
            }

            // vertical edges
            for (int x = tile_size; x < Width - tile_size; x += tile_size)
            {
                var topPos = new Vector2(Left + x, Top);
                var bottomPos = new Vector2(Left + x, Bottom - tile_size);
                _blockTexture.Draw(topPos, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(tile_size, 0, tile_size, tile_size));
                topTexture?.Draw(topPos, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(tile_size, 0, tile_size, tile_size));
                _blockTexture.Draw(bottomPos, Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(tile_size, 2 * tile_size, tile_size, tile_size));
                bottomTexture?.Draw(bottomPos + origin, origin, Color.White, Vector2.One, (float)Math.PI, new Rectangle(tile_size, 0, tile_size, tile_size));
            }
        }

        public bool TryHandlePlayer(Player player)
        {
            if (!HasPlayerRider()) return false;

            if (Edges.HasFlag(Edges.Top) && player.Top < Top && player.StateMachine.State != Player.StClimb)
            {
                // only warp the player if gravity flip succeeded
                if (player.SetGravity(GravityType.Inverted, 0f))
                    player.Top = Bottom;
            }
            else if (Edges.HasFlag(Edges.Bottom) && player.Bottom > Bottom && player.StateMachine.State != Player.StClimb)
            {
                // only warp the player if gravity flip succeeded
                if (player.SetGravity(GravityType.Normal, 0f))
                    player.Bottom = Top;
            }
            else if (Edges.HasFlag(Edges.Left) && player.Left < Left && player.StateMachine.State == Player.StClimb)
            {
                player.SetGravity(GravityType.Toggle, 0f);
                player.Left = Right;
                if (Input.MoveX <= 0) player.Facing = (Facings)(-(int)player.Facing);
            }
            else if (Edges.HasFlag(Edges.Right) && player.Right > Right && player.StateMachine.State == Player.StClimb)
            {
                player.SetGravity(GravityType.Toggle, 0f);
                player.Right = Left;
                if (Input.MoveX >= 0) player.Facing = (Facings)(-(int)player.Facing);
            }

            return true;
        }
    }
}
