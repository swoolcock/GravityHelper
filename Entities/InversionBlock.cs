// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable RedundantArgumentDefaultValue

namespace Celeste.Mod.GravityHelper.Entities
{
    [Tracked]
    [CustomEntity("GravityHelper/InversionBlock")]
    public class InversionBlock : Solid
    {
        public const string DEFAULT_SOUND = "event:/char/badeline/disappear";

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
        public bool BlockOneUse { get; }
        public bool RefillOneUse { get; }
        public int RefillDashCount { get; }
        public bool RefillStamina { get; }
        public float RefillRespawnTime { get; }
        public bool GiveGravityRefill { get; }

        private readonly bool _defaultToController;
        private string _sound;

        private readonly Sprite _sprite;
        private readonly MTexture _blockTexture;
        private readonly MTexture _edgeTexture;
        private readonly MTexture _normalEdgeTexture;
        private readonly MTexture _invertedEdgeTexture;
        private readonly MTexture _toggleEdgeTexture;
        private readonly FallingComponent _fallingComponent;
        private readonly TileGrid _tiles;
        private readonly Sprite _refillSprite;
        private readonly Image _refillOutlineImage;
        private readonly Wiggler _wiggler;

        private const float flash_time_seconds = 0.6f;
        private const float thick_line_thickness = 7f;
        private const float thin_line_thickness = 5f;
        private const float cooldown_time_seconds = 0.1f;

        private float _respawnTimeRemaining;
        private float _cooldownTimeRemaining;
        private float _flashTimeRemaining;
        private Color _flashColor;
        private Vector2 _enterPosition;
        private Vector2 _exitPosition;
        private Vector2 _shakeOffset;
        private bool _blockUsed;
        private bool _refillUsed;

        private ParticleType p_shatter;
        private ParticleType p_regen;
        private ParticleType p_glow1;
        private ParticleType p_glow2;

        private Random particlesRandom;
        private Vector2[] particles;

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
            using var _ = new PushRandomDisposable(data.ID);

            Depth = Depths.Above;

            _sprite = GFX.SpriteBank.Create("inversionBlock");
            _blockTexture = _sprite.Animations["block"].Frames[0];
            _edgeTexture = _sprite.Animations["edges"].Frames[0];
            _normalEdgeTexture = _edgeTexture.GetSubtexture(0, 0, 3 * tile_size, tile_size);
            _invertedEdgeTexture = _edgeTexture.GetSubtexture(0, tile_size, 3 * tile_size, tile_size);
            _toggleEdgeTexture = _edgeTexture.GetSubtexture(0, 2 * tile_size, 3 * tile_size, tile_size);

            if (data.Bool("autotile", true))
            {
                Add(_tiles = GFX.FGAutotiler.GenerateBox(data.Char("tiletype", '3'), data.Width / 8, data.Height / 8).TileGrid);
            }

            _defaultToController = data.Bool("defaultToController", true);
            _sound = data.Attr("sound", DEFAULT_SOUND);

            LeftGravityType = data.Enum("leftGravityType", GravityType.Toggle);
            RightGravityType = data.Enum("rightGravityType", GravityType.Toggle);
            RefillDashCount = data.Int("refillDashCount", 0);
            RefillStamina = data.Bool("refillStamina", false);
            RefillRespawnTime = data.Float("refillRespawnTime", 2.5f);
            GiveGravityRefill = data.Bool("giveGravityRefill", false);
            RefillOneUse = data.Bool("refillOneUse", false);
            BlockOneUse = data.Bool("blockOneUse", false);

            if (GiveGravityRefill)
            {
                p_shatter = GravityRefill.P_Shatter;
                p_regen = GravityRefill.P_Regen;
                p_glow1 = GravityRefill.P_Glow_Normal;
                p_glow2 = GravityRefill.P_Glow_Inverted;
                Add(_refillSprite = GFX.SpriteBank.Create("gravityRefill"));
                Add(_refillOutlineImage = new Image(GFX.Game["objects/GravityHelper/gravityRefill/outline"]));
            }
            else if (RefillDashCount > 0)
            {
                if (RefillDashCount == 2)
                {
                    p_shatter = Refill.P_ShatterTwo;
                    p_regen = Refill.P_RegenTwo;
                    p_glow1 = Refill.P_GlowTwo;
                    Add(_refillSprite = new Sprite(GFX.Game, "objects/refillTwo/idle"));
                    Add(_refillOutlineImage = new Image(GFX.Game["objects/refillTwo/outline"]));
                }
                else
                {
                    p_shatter = Refill.P_Shatter;
                    p_regen = Refill.P_Regen;
                    p_glow1 = Refill.P_Glow;
                    Add(_refillSprite = new Sprite(GFX.Game, "objects/refill/idle"));
                    Add(_refillOutlineImage = new Image(GFX.Game["objects/refill/outline"]));
                }
                _refillSprite.AddLoop("idle", "", 0.1f);
                _refillSprite.Play("idle");
                _refillSprite.CenterOrigin();
            }

            if (_refillSprite != null && _refillOutlineImage != null)
            {
                _refillOutlineImage.Visible = false;
                _refillOutlineImage.Position = _refillSprite.Position = new Vector2(Width / 2, Height / 2);
                _refillOutlineImage.CenterOrigin();
                Add(_wiggler = Wiggler.Create(1f, 4f, v => _refillSprite.Scale = Vector2.One * (1f + v * 0.2f)));

                int w = (data.Width / 8) - 2;
                int h = (data.Height / 8) - 2;
                if (w > 0 && h > 0)
                {
                    particlesRandom = new Random(Calc.Random.Next());
                    particles = new Vector2[w * h];
                    for (int i = 0; i < particles.Length; i++)
                    {
                        float x = (i % w) * 8 + 8;
                        float y = (i / w) * 8 + 8;
                        particles[i] = new Vector2(x + particlesRandom.NextFloat(8), y + particlesRandom.NextFloat(8));
                    }
                }
            }

            Edges |= data.Bool("leftEnabled") ? Edges.Left : Edges.None;
            Edges |= data.Bool("rightEnabled") ? Edges.Right : Edges.None;
            Edges |= data.Bool("topEnabled", true) ? Edges.Top : Edges.None;
            Edges |= data.Bool("bottomEnabled", true) ? Edges.Bottom : Edges.None;

            var fallType = (FallingComponent.FallingType)data.Int("fallType", (int)FallingComponent.FallingType.None);
            if (data.Bool("fall", false)) fallType = data.Bool("fallUp", false) ? FallingComponent.FallingType.Up : FallingComponent.FallingType.Down;

            if (fallType != FallingComponent.FallingType.None)
            {
                Add(_fallingComponent = new FallingComponent
                {
                    ClimbFall = data.Bool("climbFall", true),
                    FallType = fallType,
                    EndOnSolidTiles = data.Bool("endFallOnSolidTiles", true),
                });
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (_defaultToController && Scene.GetActiveController<SoundGravityController>() is { } soundController)
            {
                _sound = soundController.InversionBlockSound;
            }
        }

        private MTexture textureForGravityType(GravityType type) => type switch
        {
            GravityType.Normal => _normalEdgeTexture,
            GravityType.Inverted => _invertedEdgeTexture,
            GravityType.Toggle => _toggleEdgeTexture,
            _ => null,
        };

        private void respawnRefill()
        {
            _refillSprite.Visible = true;
            _refillOutlineImage.Visible = false;
            _wiggler.Start();
            Audio.Play(RefillDashCount == 2 ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", Position);
            SceneAs<Level>().ParticlesFG.Emit(p_regen, 16, Center, Vector2.One * 2f);
        }

        public override void OnShake(Vector2 amount) => _shakeOffset += amount;

        public override void Update()
        {
            base.Update();

            if (_flashTimeRemaining > 0) _flashTimeRemaining -= Engine.DeltaTime;
            if (_cooldownTimeRemaining > 0) _cooldownTimeRemaining -= Engine.DeltaTime;

            if (_respawnTimeRemaining > 0)
            {
                _respawnTimeRemaining -= Engine.DeltaTime;
                if (_respawnTimeRemaining <= 0 && (!_refillUsed || !RefillOneUse))
                    respawnRefill();
            }

            emitParticles();
        }

        private void emitParticles()
        {
            if (_refillSprite?.Visible == true && particles.Length > 0 && particlesRandom != null && Scene.OnInterval(0.2f))
            {
                foreach (var particle in particles)
                {
                    const float chance = 0.5f;
                    const int amount = 2;
                    if (particlesRandom.NextFloat() < chance)
                    {
                        var pos = Position + particle;
                        if (p_glow2 != null)
                        {
                            SceneAs<Level>().ParticlesFG.Emit(p_glow1, amount / 2, pos, Vector2.One * 4f, Vector2.UnitY.Angle());
                            SceneAs<Level>().ParticlesFG.Emit(p_glow2, amount / 2, pos, Vector2.One * 4f, (-Vector2.UnitY).Angle());
                        }
                        else
                        {
                            SceneAs<Level>().ParticlesFG.Emit(p_glow1, amount, pos, Vector2.One * 4f, (pos - Center).Angle());
                        }
                    }
                }
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
            var expired = _blockUsed && BlockOneUse;
            var centreColor = expired ? inactiveColor : Color.White;
            var leftColor = activeEdges.HasFlag(Edges.Left) ? activeColor : inactiveColor;
            var rightColor = activeEdges.HasFlag(Edges.Right) ? activeColor : inactiveColor;
            var topColor = activeEdges.HasFlag(Edges.Top) ? activeColor : inactiveColor;
            var bottomColor = activeEdges.HasFlag(Edges.Bottom) ? activeColor : inactiveColor;
            int widthInTiles = (int) (Width / tile_size);
            int heightInTiles = (int) (Height / tile_size);

            // draw 9-patch if we don't have an autotile
            if (_tiles == null)
            {
                for (int y = 0; y < heightInTiles; y++)
                {
                    for (int x = 0; x < widthInTiles; x++)
                    {
                        var srcX = x == 0 ? 0 : x == widthInTiles - 1 ? 16 : 8;
                        var srcY = y == 0 ? 0 : y == heightInTiles - 1 ? 16 : 8;
                        _blockTexture.Draw(new Vector2(Left + x * tile_size, Top + y * tile_size), Vector2.Zero, centreColor, Vector2.One, 0f, new Rectangle(srcX, srcY, tile_size, tile_size));
                    }
                }
            }
            else
            {
                _tiles.Render();
                _tiles.Color = centreColor;
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

            // draw refill crystal if we have it
            if (_refillSprite?.Visible == true) _refillSprite.Render();
            if (_refillOutlineImage?.Visible == true) _refillOutlineImage.Render();

            // only draw the edges if it's not expired
            if (!expired)
            {
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

            Position -= _shakeOffset;
        }

        private bool hasPlayerRiderOrBuffered(Player player)
        {
            // if we're actually riding then it's fine
            if (HasPlayerRider()) return true;

            // only allow these states
            switch (player.StateMachine.State)
            {
                case Player.StNormal:
                case Player.StDash:
                case Player.StRedDash:
                case Player.StStarFly:
                case Player.StHitSquash:
                case Player.StClimb:
                    break;
                default:
                    return false;
            }

            // we can't be holding
            if (player.Holding != null) return false;

            // we must be grabbing
            if (!Input.GrabCheck) return false;

            // we can't be comf
            if (!player.CanUnDuck) return false;

            // we must have stamina
            if (player.Stamina <= 0) return false;

            const float buffer = 3f;

            // if we're on the left, facing right, and within a few pixels
            var activeEdges = ActiveEdges;
            if (activeEdges.HasFlag(Edges.Left) &&
                player.Right <= Left &&
                player.Facing == Facings.Right &&
                player.CollideCheck(this, player.Position + Vector2.UnitX * buffer) &&
                !ClimbBlocker.Check(Scene, player, player.Position + Vector2.UnitX * buffer))
                return true;

            // if we're on the right, facing left, and within a few pixels
            if (activeEdges.HasFlag(Edges.Right) &&
                player.Left >= Right &&
                player.Facing == Facings.Left &&
                player.CollideCheck(this, player.Position - Vector2.UnitX * buffer) &&
                !ClimbBlocker.Check(Scene, player, player.Position - Vector2.UnitX * buffer))
                return true;

            return false;
        }

        public bool TryHandlePlayer(Player player)
        {
            if (_cooldownTimeRemaining > 0) return false;
            if (_blockUsed && BlockOneUse) return false;

            if (!hasPlayerRiderOrBuffered(player) || Scene is not Level level) return false;

            var currentGravity = player.GetGravity();
            var activeEdges = ActiveEdges;
            var oldPosition = player.Center;
            var direction = Vector2.Zero;
            var exitPoint = Vector2.Zero;
            var inType = GravityType.Normal;
            var outType = GravityType.Inverted;

            if (activeEdges.HasFlag(Edges.Top) && player.Bottom <= Top && player.StateMachine.State != Player.StClimb)
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
            else if (activeEdges.HasFlag(Edges.Bottom) && player.Top >= Bottom && player.StateMachine.State != Player.StClimb)
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
            else if (activeEdges.HasFlag(Edges.Left) && player.Right <= Left)
            {
                // check whether we have space to move on the other side
                var targetX = Right - player.Collider.Left;
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
            else if (activeEdges.HasFlag(Edges.Right) && player.Left >= Right)
            {
                // check whether we have space to move on the other side
                var targetX = Left - player.Collider.Right;
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
            if (!string.IsNullOrWhiteSpace(_sound))
                Audio.Play(_sound, player.Position);

            // clamp line
            var lineOffset = (int)(thick_line_thickness / 2f);

            if (_enterPosition.X == _exitPosition.X)
                _exitPosition.X = _enterPosition.X = Calc.Clamp(_enterPosition.X, Left + lineOffset + 1, Right - lineOffset);
            else if (_enterPosition.Y == _exitPosition.Y)
                _exitPosition.Y = _enterPosition.Y = Calc.Clamp(_enterPosition.Y, Top + lineOffset + 1, Bottom - lineOffset);

            // flash
            _flashTimeRemaining = flash_time_seconds;
            _flashColor = player.GetGravity().Color();

            // cooldown
            _cooldownTimeRemaining = cooldown_time_seconds;
            _blockUsed = true;

            // always refill stamina if required
            if (RefillStamina) player.RefillStamina();

            // refill dash and provide gravity refill if required and not single use
            if (_respawnTimeRemaining <= 0 && (!_refillUsed || !RefillOneUse))
            {
                var targetDashes = Math.Max(RefillDashCount, player.Dashes);
                var targetGravityRefills = Math.Max(GravityRefill.NumberOfCharges, GiveGravityRefill ? 1 : 0);
                var staminaWarning = player.Stamina < 20;

                if (targetDashes > player.Dashes || targetGravityRefills > GravityRefill.NumberOfCharges || staminaWarning)
                {
                    _refillUsed = true;
                    _respawnTimeRemaining = RefillRespawnTime;
                    player.Dashes = targetDashes;
                    player.RefillStamina();
                    GravityRefill.NumberOfCharges = targetGravityRefills;
                    Audio.Play(RefillDashCount == 2 ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
                    _refillSprite.Visible = false;

                    float particleDirection = player.Speed.Angle();
                    level.ParticlesFG.Emit(p_shatter, 5, Center, Vector2.One * 4f, particleDirection - (float)Math.PI / 2f);
                    level.ParticlesFG.Emit(p_shatter, 5, Center, Vector2.One * 4f, particleDirection + (float)Math.PI / 2f);
                    SlashFx.Burst(Center, particleDirection);

                    if (!RefillOneUse)
                        _refillOutlineImage.Visible = true;
                }
            }

            // we handled it
            return true;
        }
    }
}
