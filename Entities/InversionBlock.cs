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
        private const int tile_size = 8;

        private readonly ParticleType _normalInvertedParticleType = new ParticleType(Player.P_Split)
        {
            Color = GravityType.Normal.Color().Lighter(),
            Color2 = GravityType.Inverted.Color().Lighter(),
            DirectionRange = (float)Math.PI / 4f,
        };

        private readonly ParticleType _normalToggleParticleType = new ParticleType(Player.P_Split)
        {
            Color = GravityType.Normal.Color().Lighter(),
            Color2 = GravityType.Toggle.Color().Lighter(),
            DirectionRange = (float)Math.PI / 4f,
        };

        private readonly ParticleType _invertedToggleParticleType = new ParticleType(Player.P_Split)
        {
            Color = GravityType.Inverted.Color().Lighter(),
            Color2 = GravityType.Toggle.Color().Lighter(),
            DirectionRange = (float)Math.PI / 4f,
        };

        private readonly ParticleType _toggleToggleParticleType = new ParticleType(Player.P_Split)
        {
            Color = GravityType.Toggle.Color(),
            Color2 = GravityType.Toggle.Color().Lighter(),
            DirectionRange = (float)Math.PI / 4f,
        };

        private ParticleType particleTypeForGravity(GravityType inType, GravityType outType)
        {
            if (inType == GravityType.Inverted && outType == GravityType.Normal || inType == GravityType.Normal && outType == GravityType.Inverted)
                return _normalInvertedParticleType;
            if (inType == GravityType.Inverted && outType == GravityType.Toggle || inType == GravityType.Toggle && outType == GravityType.Inverted)
                return _invertedToggleParticleType;
            if (inType == GravityType.Normal && outType == GravityType.Toggle || inType == GravityType.Toggle && outType == GravityType.Normal)
                return _normalToggleParticleType;
            if (inType == GravityType.Toggle && outType == GravityType.Toggle)
                return _toggleToggleParticleType;
            return null;
        }

        public Edges Edges { get; }
        public GravityType LeftGravityType { get; }
        public GravityType RightGravityType { get; }

        private readonly MTexture _blockTexture;
        private readonly MTexture _edgeTexture;
        private readonly MTexture _normalEdgeTexture;
        private readonly MTexture _invertedEdgeTexture;
        private readonly MTexture _toggleEdgeTexture;

        public Edges ActiveEdges
        {
            get
            {
                if (GravityHelperModule.PlayerComponent is not { } playerComponent)
                    return Edges.None;
                if (playerComponent.Locked)
                    return Edges.None;

                var currentGravity = playerComponent.CurrentGravity;

                // start with the enabled edges
                var activeEdges = Edges;

                // if we're normal gravity we can't use the bottom
                if (currentGravity == GravityType.Normal)
                    activeEdges &= ~Edges.Bottom;
                // if we're inverted gravity we can't use the top
                else if (currentGravity == GravityType.Inverted)
                    activeEdges &= ~Edges.Top;
                // if we don't match the left (and it's not toggle), we can't use left
                if (currentGravity != LeftGravityType && LeftGravityType != GravityType.Toggle)
                    activeEdges &= ~Edges.Left;
                // if we don't match the right (and it's not toggle), we can't use right
                if (currentGravity != RightGravityType && RightGravityType != GravityType.Toggle)
                    activeEdges &= ~Edges.Right;

                return activeEdges;
            }
        }

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
            var leftTexture = !Edges.HasFlag(Edges.Left) ? null : textureForGravityType(LeftGravityType);
            var rightTexture = !Edges.HasFlag(Edges.Right) ? null : textureForGravityType(RightGravityType);
            var topTexture = !Edges.HasFlag(Edges.Top) ? null : _normalEdgeTexture;
            var bottomTexture = !Edges.HasFlag(Edges.Bottom) ? null : _invertedEdgeTexture;
            var origin = new Vector2(tile_size / 2f, tile_size / 2f);
            var activeEdges = ActiveEdges;
            var activeColor = Color.White;
            var inactiveColor = new Color(0.5f, 0.5f, 0.5f);
            var leftColor = activeEdges.HasFlag(Edges.Left) ? activeColor : inactiveColor;
            var rightColor = activeEdges.HasFlag(Edges.Right) ? activeColor : inactiveColor;
            var topColor = activeEdges.HasFlag(Edges.Top) ? activeColor : inactiveColor;
            var bottomColor = activeEdges.HasFlag(Edges.Bottom) ? activeColor : inactiveColor;
            int widthInTiles = (int) (Width / tile_size);
            int heightInTiles = (int) (Height / tile_size);

            // draw 9-patch
            for (int y = 0; y < heightInTiles; y++)
            {
                for (int x = 0; x < widthInTiles; x++)
                {
                    var srcX = x == 0 ? 0 : x == widthInTiles - 1 ? 16 : 8;
                    var srcY = y == 0 ? 0 : y == heightInTiles - 1 ? 16 : 8;
                    _blockTexture.Draw(new Vector2(Left + x * tile_size, Top + y * tile_size), Vector2.Zero, Color.White, Vector2.One, 0f, new Rectangle(srcX, srcY, tile_size, tile_size));
                }
            }

            // top left corner
            var position = TopLeft;
            leftTexture?.Draw(position + origin, origin, leftColor, Vector2.One, (float)-Math.PI / 2, new Rectangle(2 * tile_size, 0, tile_size, tile_size));
            topTexture?.Draw(position, Vector2.Zero, topColor, Vector2.One, 0f, new Rectangle(0, 0, tile_size, tile_size));

            // top right corner
            position = TopRight - Vector2.UnitX * tile_size;
            rightTexture?.Draw(position + origin, origin, rightColor, Vector2.One, (float)Math.PI / 2, new Rectangle(0, 0, tile_size, tile_size));
            topTexture?.Draw(position, Vector2.Zero, topColor, Vector2.One, 0f, new Rectangle(2 * tile_size, 0, tile_size, tile_size));

            // bottom left corner
            position = BottomLeft - Vector2.UnitY * tile_size;
            leftTexture?.Draw(position + origin, origin, leftColor, Vector2.One, (float)-Math.PI / 2, new Rectangle(0, 0, tile_size, tile_size));
            bottomTexture?.Draw(position + origin, origin, bottomColor, Vector2.One, (float)Math.PI, new Rectangle(2 * tile_size, 0, tile_size, tile_size));

            // bottom right corner
            position = BottomRight - Vector2.One * tile_size;
            rightTexture?.Draw(position + origin, origin, rightColor, Vector2.One, (float)Math.PI / 2, new Rectangle(2 * tile_size, 0, tile_size, tile_size));
            bottomTexture?.Draw(position + origin, origin, bottomColor, Vector2.One, (float)Math.PI, new Rectangle(0, 0, tile_size, tile_size));

            // horizontal edges
            for (int y = tile_size; y < Height - tile_size; y += tile_size)
            {
                var leftPos = new Vector2(Left, Top + y);
                var rightPos = new Vector2(Right - tile_size, Top + y);
                leftTexture?.Draw(leftPos + origin, origin, leftColor, Vector2.One, (float)-Math.PI / 2, new Rectangle(tile_size, 0, tile_size, tile_size));
                rightTexture?.Draw(rightPos + origin, origin, rightColor, Vector2.One, (float)Math.PI / 2, new Rectangle(tile_size, 0, tile_size, tile_size));
            }

            // vertical edges
            for (int x = tile_size; x < Width - tile_size; x += tile_size)
            {
                var topPos = new Vector2(Left + x, Top);
                var bottomPos = new Vector2(Left + x, Bottom - tile_size);
                topTexture?.Draw(topPos, Vector2.Zero, topColor, Vector2.One, 0f, new Rectangle(tile_size, 0, tile_size, tile_size));
                bottomTexture?.Draw(bottomPos + origin, origin, bottomColor, Vector2.One, (float)Math.PI, new Rectangle(tile_size, 0, tile_size, tile_size));
            }
        }

        public bool TryHandlePlayer(Player player)
        {
            if (!HasPlayerRider() || Scene is not Level level) return false;

            var currentGravity = player.GetGravity();
            var activeEdges = ActiveEdges;
            var oldPosition = player.Center;
            var direction = Vector2.Zero;
            var exitPoint = Vector2.Zero;
            var inType = GravityType.Normal;
            var outType = GravityType.Inverted;

            if (activeEdges.HasFlag(Edges.Top) && player.Top < Top && player.StateMachine.State != Player.StClimb)
            {
                // check whether we have space to move on the other side
                if (player.CollideCheck<Solid>(new Vector2(player.X, Bottom + player.Height)))
                    return false;

                // bail if gravity flip failed for some reason
                if (!player.SetGravity(GravityType.Inverted, 0f))
                    return false;

                // move us
                player.Top = Bottom;

                // configure effects
                direction = Vector2.UnitY;
                exitPoint = player.TopCenter;
                inType = GravityType.Normal;
                outType = GravityType.Inverted;
            }
            else if (activeEdges.HasFlag(Edges.Bottom) && player.Bottom > Bottom && player.StateMachine.State != Player.StClimb)
            {
                // check whether we have space to move on the other side
                if (player.CollideCheck<Solid>(new Vector2(player.X, Top - player.Height)))
                    return false;

                // bail if gravity flip failed for some reason
                if (!player.SetGravity(GravityType.Normal, 0f))
                    return false;

                // move us
                player.Bottom = Top;

                // configure effects
                direction = -Vector2.UnitY;
                exitPoint = player.BottomCenter;
                inType = GravityType.Inverted;
                outType = GravityType.Normal;
            }
            else if (activeEdges.HasFlag(Edges.Left) && player.Left < Left && player.StateMachine.State == Player.StClimb)
            {
                // check whether we have space to move on the other side
                var targetX = player.X + player.Width + Width;
                var targetY = currentGravity == GravityType.Normal ? player.Bottom : player.Top;
                if (player.CollideCheck<Solid>(new Vector2(targetX, targetY)))
                    return false;

                // bail if gravity flip failed for some reason
                if (!player.SetGravity(GravityType.Toggle, 0f))
                    return false;

                // change facing if we're not pushing into the block
                if (Input.MoveX <= 0)
                    player.Facing = (Facings)(-(int)player.Facing);

                // move us
                player.Left = Right;

                // configure effects
                direction = Vector2.UnitX;
                exitPoint = player.CenterLeft;
                inType = LeftGravityType;
                outType = Edges.HasFlag(Edges.Right) ? RightGravityType : LeftGravityType.Opposite();
            }
            else if (Edges.HasFlag(Edges.Right) && player.Right > Right && player.StateMachine.State == Player.StClimb)
            {
                // check whether we have space to move on the other side
                var targetX = player.X - player.Width - Width;
                var targetY = currentGravity == GravityType.Normal ? player.Bottom : player.Top;
                if (player.CollideCheck<Solid>(new Vector2(targetX, targetY)))
                    return false;

                // bail if gravity flip failed for some reason
                if (!player.SetGravity(GravityType.Toggle, 0f))
                    return false;

                // change facing if we're not pushing into the block
                if (Input.MoveX >= 0)
                    player.Facing = (Facings)(-(int)player.Facing);

                // move us
                player.Right = Left;

                // configure effects
                direction = -Vector2.UnitX;
                exitPoint = player.CenterRight;
                inType = RightGravityType;
                outType = Edges.HasFlag(Edges.Left) ? LeftGravityType : RightGravityType.Opposite();
            }
            else
            {
                // just fail
                return false;
            }

            // add displacements
            level.Displacement.AddBurst(oldPosition, 0.35f, 8f, 48f, 0.25f);
            level.Displacement.AddBurst(player.Center, 0.35f, 8f, 48f, 0.25f);

            // emit particles
            var particleType = particleTypeForGravity(inType, outType);
            level.Particles.Emit(particleType, 16, exitPoint, Vector2.One * 8f, direction.Angle());

            // play a sound
            Audio.Play("event:/char/badeline/disappear", player.Position);

            // we handled it
            return true;
        }
    }
}
