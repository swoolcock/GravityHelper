// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

internal class FallingComponent : Component
{
    // actions
    public Func<bool> PlayerFallCheck;
    public Func<bool> PlayerWaitCheck;
    public Action ShakeSfx;
    public Action ImpactSfx;
    public Action LandParticles;
    public Action FallParticles;

    // config
#pragma warning disable CS0649
    public float FallDelay;
#pragma warning restore CS0649
    public bool ShouldRumble = true;
    public bool ClimbFall = true;
    public float FallSpeed = 160f;
    public bool ShouldManageSafe = true;
    public FallingType FallType = FallingType.Down;
    public bool EndOnSolidTiles = true;

    // coroutine properties
    public bool Triggered;
    public bool HasStartedFalling { get; private set; }

    // ReSharper disable once NotAccessedField.Local
    private Coroutine _coroutine;
    private float _fallDelayRemaining;
    private bool _fallingUp;

    public new Solid Entity => base.Entity as Solid;

    public FallingComponent() : base(false, false)
    {
        PlayerFallCheck = playerFallCheck;
        PlayerWaitCheck = playerWaitCheck;
        ShakeSfx = shakeSfx;
        ImpactSfx = impactSfx;
        LandParticles = landParticles;
        FallParticles = fallParticles;
    }

    public override void Added(Entity entity)
    {
        if (entity is not Solid)
            throw new Exception("FallingComponent should only be used on Solids!");

        base.Added(entity);

        entity.Add(_coroutine = new Coroutine(fallingSequence()));
        if (ShouldManageSafe) Entity.Safe = false;
    }

    private bool playerFallCheck()
    {
        if (Entity == null) return false;
        return ClimbFall ? Entity.HasPlayerRider() : Entity.HasPlayerOnTop();
    }

    private bool playerWaitCheck()
    {
        if (Entity == null) return false;
        if (Triggered || PlayerFallCheck?.Invoke() == true)
            return true;
        if (!ClimbFall)
            return false;
        return Entity.CollideCheck<Player>(Entity.Position - Vector2.UnitX) || Entity.CollideCheck<Player>(Entity.Position + Vector2.UnitX);
    }

    private void shakeSfx()
    {
        if (Entity != null)
            Audio.Play("event:/game/general/fallblock_shake", Entity.Center);
    }

    private void impactSfx()
    {
        if (Entity != null)
            Audio.Play("event:/game/general/fallblock_impact", Entity.BottomCenter);
    }

    private void fallParticles()
    {
        if (Entity == null) return;
        var level = Entity.SceneAs<Level>();
        for (int x = 2; x < Entity.Width; x += 4)
        {
            var position = new Vector2(Entity.X + x, Entity.Y);
            var range = Vector2.One * 4f;
            var direction = (float)Math.PI / 2f;
            var offset = new Vector2(x, -2f);
            var check = _fallingUp ? Entity.BottomLeft - offset : Entity.TopLeft + offset;
            if (level.CollideCheck<Solid>(check))
                level.Particles.Emit(FallingBlock.P_FallDustA, 2, position, range, _fallingUp ? -direction : direction);
            level.Particles.Emit(FallingBlock.P_FallDustB, 2, position, range);
        }
    }

    private void landParticles()
    {
        if (Entity == null) return;
        var level = Entity.SceneAs<Level>();
        for (int x = 2; x <= Entity.Width; x += 4)
        {
            var offset = new Vector2(x, 3f);
            var checkPosition = _fallingUp ? Entity.TopLeft - offset : Entity.BottomLeft + offset;
            if (level.CollideCheck<Solid>(checkPosition))
            {
                var position = new Vector2(Entity.X + x, _fallingUp ? Entity.Top : Entity.Bottom);
                var range = Vector2.One * 4f;
                var fallDustDirection = -(float)Math.PI / 2f;
                level.ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, position, range, _fallingUp ? -fallDustDirection : fallDustDirection);
                var landDustDirection = x >= Entity.Width / 2f ? 0f : (float)Math.PI;
                level.ParticlesFG.Emit(FallingBlock.P_LandDust, 1, position, range, _fallingUp ? -landDustDirection : landDustDirection);
            }
        }
    }

    private IEnumerator fallingSequence()
    {
        // cache things
        var self = this;
        var entity = self.Entity;
        var level = entity?.SceneAs<Level>();

        // unlikely but safety
        if (entity == null) yield break;

        // reset things
        if (self.ShouldManageSafe) entity.Safe = false;
        self.Triggered = false;
        self.HasStartedFalling = false;

        // wait until we should fall
        while (!self.Triggered && self.PlayerFallCheck?.Invoke() != true)
            yield return null;

        // wait for the delay
        self._fallDelayRemaining = self.FallDelay;
        while (self._fallDelayRemaining > 0)
        {
            self._fallDelayRemaining -= Engine.DeltaTime;
            yield return null;
        }

        self.HasStartedFalling = true;

        // loop forever
        while (true)
        {
            // determine whether we should fall up
            _fallingUp = FallType == FallingType.Up || FallType == FallingType.MatchPlayer && GravityHelperModule.ShouldInvertPlayer || FallType == FallingType.OppositePlayer && !GravityHelperModule.ShouldInvertPlayer;

            // start shaking
            self.ShakeSfx?.Invoke();
            entity.StartShaking();
            if (self.ShouldRumble) Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            // shake for a while
            for (float timer = 0.4f; timer > 0 && self.PlayerWaitCheck?.Invoke() != false; timer -= Engine.DeltaTime)
                yield return null;

            // stop shaking
            entity.StopShaking();

            // particles
            self.FallParticles?.Invoke();

            // fall
            float speed = 0f;
            float maxSpeed = self.FallSpeed;
            while (true)
            {
                // update the speed
                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                // try to move
                if (!entity.MoveVCollideSolids(speed * Engine.DeltaTime * (_fallingUp ? -1 : 1), true))
                {
                    // if we've fallen out the bottom of the screen, we should remove the entity
                    // otherwise yield for a frame and loop
                    if (!_fallingUp && entity.Top <= level.Bounds.Bottom + 16 && (entity.Top <= level.Bounds.Bottom - 1 || !entity.CollideCheck<Solid>(entity.Position + Vector2.UnitY)) ||
                        _fallingUp && entity.Bottom >= level.Bounds.Top - 16 && (entity.Bottom >= level.Bounds.Top + 1 || !entity.CollideCheck<Solid>(entity.Position - Vector2.UnitY)))
                        yield return null;
                    else
                    {
                        // we've fallen out of the screen and should remove the entity
                        entity.Collidable = entity.Visible = false;
                        yield return 0.2f;
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(entity.Center.X, _fallingUp ? (entity.Top - 12f) : (entity.Bottom + 12f))))
                        {
                            yield return 0.2f;
                            level.Shake();
                            if (ShouldRumble) Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }

                        entity.RemoveSelf();
                        entity.DestroyStaticMovers();
                        yield break;
                    }
                }
                else
                {
                    // if we hit something, break
                    break;
                }
            }

            // impact effects
            self.ImpactSfx?.Invoke();
            if (self.ShouldRumble) Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            level.DirectionalShake(_fallingUp ? -Vector2.UnitY : Vector2.UnitY);
            entity.StartShaking();
            self.LandParticles?.Invoke();
            yield return 0.2f;
            entity.StopShaking();

            // if it's hit the fg tiles then make it safe and end
            if (EndOnSolidTiles && entity.CollideCheck<SolidTiles>(entity.Position + (_fallingUp ? -Vector2.UnitY : Vector2.UnitY)))
            {
                entity.Safe |= self.ShouldManageSafe;
                yield break;
            }

            // wait until we can fall again
            while (entity.CollideCheck<Platform>(entity.Position + (_fallingUp ? -Vector2.UnitY : Vector2.UnitY)))
            {
                yield return 0.1f;
                // if the block is dependent on the player's gravity and the player is able to trigger it, update _fallingUp
                if (FallType is FallingType.MatchPlayer or FallingType.OppositePlayer && PlayerFallCheck?.Invoke() != false)
                    _fallingUp = FallType == FallingType.MatchPlayer && GravityHelperModule.ShouldInvertPlayer ||
                        FallType == FallingType.OppositePlayer && !GravityHelperModule.ShouldInvertPlayer;
            }
        }
    }

    public enum FallingType
    {
        None,
        Down,
        Up,
        MatchPlayer,
        OppositePlayer,
    }
}
