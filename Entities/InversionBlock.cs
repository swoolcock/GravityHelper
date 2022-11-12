// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
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

        private ParticleType particleTypeForGravity(GravityType inType, GravityType outType) => inType switch
        {
            GravityType.Toggle when outType == GravityType.Normal => _normalToggleParticleType,
            GravityType.Toggle when outType == GravityType.Inverted => _invertedToggleParticleType,
            _ => _normalInvertedParticleType,
        };

        public Edges Edges { get; }
        public GravityType LeftGravityType { get; }
        public GravityType RightGravityType { get; }

        private readonly MTexture _blockTexture;
        private readonly MTexture _edgeTexture;
        private readonly MTexture _normalEdgeTexture;
        private readonly MTexture _invertedEdgeTexture;
        private readonly MTexture _toggleEdgeTexture;
        private readonly FallingComponent _fallingComponent;

        private const float flash_time_seconds = 0.6f;
        private const float thick_line_thickness = 7f;
        private const float thin_line_thickness = 5f;

        private float _flashTimeRemaining = 0f;
        private Color _flashColor;
        private Vector2 _enterPosition;
        private Vector2 _exitPosition;
        private Vector2 _shakeOffset;

        private Edges _activeEdges;
        public Edges ActiveEdges
        {
            get
            {
                if (GravityHelperModule.PlayerComponent is not { } playerComponent)
                    return _activeEdges;

                if (playerComponent.Locked)
                    return _activeEdges = Edges.None;

                var currentGravity = playerComponent.CurrentGravity;

                // start with the enabled edges
                _activeEdges = Edges;

                // if we're normal gravity we can't use the bottom
                if (currentGravity == GravityType.Normal)
                    _activeEdges &= ~Edges.Bottom;
                // if we're inverted gravity we can't use the top
                else if (currentGravity == GravityType.Inverted)
                    _activeEdges &= ~Edges.Top;
                // if we don't match the left (and it's not toggle), we can't use left
                if (currentGravity != LeftGravityType && LeftGravityType != GravityType.Toggle)
                    _activeEdges &= ~Edges.Left;
                // if we don't match the right (and it's not toggle), we can't use right
                if (currentGravity != RightGravityType && RightGravityType != GravityType.Toggle)
                    _activeEdges &= ~Edges.Right;

                return _activeEdges;
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

            if (data.Bool("fall"))
                Add(_fallingComponent = new FallingComponent { ClimbFall = data.Bool("climbFall", true) });
        }

        private MTexture textureForGravityType(GravityType type) => type switch
        {
            GravityType.Normal => _normalEdgeTexture,
            GravityType.Inverted => _invertedEdgeTexture,
            GravityType.Toggle => _toggleEdgeTexture,
            _ => null,
        };

        public override void OnShake(Vector2 amount) => _shakeOffset += amount;

        public override void Update()
        {
            base.Update();

            if (_flashTimeRemaining > 0)
            {
                _flashTimeRemaining -= Engine.DeltaTime;
            }
        }

        public override void Render()
        {
            Position += _shakeOffset;

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

            // draw flash
            if (_flashTimeRemaining > 0)
            {
                var progress = _flashTimeRemaining / flash_time_seconds;
                Draw.Rect(X, Y, Width, Height, _flashColor * (progress * 0.3f));
                var beamStart = Calc.LerpSnap(_enterPosition, _exitPosition, Ease.QuintOut(1 - progress)).Round();
                if ((_exitPosition - beamStart).LengthSquared() > 2)
                {
                    Draw.Line(beamStart, _exitPosition, _flashColor * 0.3f, thick_line_thickness);
                    Draw.Line(beamStart, _exitPosition, _flashColor * 0.3f, thin_line_thickness);
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

            Position -= _shakeOffset;
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
                _enterPosition = player.BottomCenter;
                player.Top = Bottom;
                _exitPosition = player.TopCenter;

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
                _enterPosition = player.TopCenter;
                player.Bottom = Top;
                _exitPosition = player.BottomCenter;

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
                _enterPosition = player.CenterRight;
                player.Left = Right;
                _exitPosition = player.CenterLeft;

                // configure effects
                direction = Vector2.UnitX;
                exitPoint = player.CenterLeft;
                inType = LeftGravityType;
                outType = !Edges.HasFlag(Edges.Right) || RightGravityType != GravityType.Toggle ? player.GetGravity() : GravityType.Toggle;
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
                _enterPosition = player.CenterLeft;
                player.Right = Left;
                _exitPosition = player.CenterRight;

                // configure effects
                direction = -Vector2.UnitX;
                exitPoint = player.CenterRight;
                inType = RightGravityType;
                outType = !Edges.HasFlag(Edges.Left) || LeftGravityType != GravityType.Toggle ? player.GetGravity() : GravityType.Toggle;
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
            var particleType = particleTypeForGravity(inType, player.GetGravity());
            level.Particles.Emit(particleType, 16, exitPoint, Vector2.One * 8f, direction.Angle());

            // play a sound
            Audio.Play("event:/char/badeline/disappear", player.Position);

            // clamp line
            var lineOffset = (int)(thick_line_thickness / 2f);

            if (_enterPosition.X == _exitPosition.X)
                _exitPosition.X = _enterPosition.X = Calc.Clamp(_enterPosition.X, Left + lineOffset + 1, Right - lineOffset);
            else if (_enterPosition.Y == _exitPosition.Y)
                _exitPosition.Y = _enterPosition.Y = Calc.Clamp(_enterPosition.Y, Top + lineOffset + 1, Bottom - lineOffset);

            // flash
            _flashTimeRemaining = flash_time_seconds;
            _flashColor = player.GetGravity().Color();

            // we handled it
            return true;
        }
    }
}
