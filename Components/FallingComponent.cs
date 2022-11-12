// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components
{
    public class FallingComponent : Component
    {
        // actions
        public Func<bool> PlayerFallCheck;
        public Func<bool> PlayerWaitCheck;
        public Action ShakeSfx;
        public Action ImpactSfx;
        public Action LandParticles;
        public Action FallParticles;

        // config
        public float FallDelay;
        public bool ShouldRumble = true;
        public bool ClimbFall = true;
        public float FallSpeed = 160f;
        public bool ShouldManageSafe = true;

        // coroutine properties
        public bool Triggered;
        public bool HasStartedFalling { get; private set; }

        private Coroutine _coroutine;
        private float _fallDelayRemaining;

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
                if (level.CollideCheck<Solid>(Entity.TopLeft + new Vector2(x, -2f)))
                    level.Particles.Emit(FallingBlock.P_FallDustA, 2, position, range, (float)Math.PI / 2f);
                level.Particles.Emit(FallingBlock.P_FallDustB, 2, position, range);
            }
        }

        private void landParticles()
        {
            if (Entity == null) return;
            var level = Entity.SceneAs<Level>();
            for (int x = 2; x <= Entity.Width; x += 4)
            {
                if (level.CollideCheck<Solid>(Entity.BottomLeft + new Vector2(x, 3f)))
                {
                    var position = new Vector2(Entity.X + x, Entity.Bottom);
                    var range = Vector2.One * 4f;
                    level.ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, position, range, -(float)Math.PI / 2f);
                    float direction = x >= Entity.Width / 2f ? 0f : (float)Math.PI;
                    level.ParticlesFG.Emit(FallingBlock.P_LandDust, 1, position, range, direction);
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
                    if (!entity.MoveVCollideSolids(speed * Engine.DeltaTime, true))
                    {
                        // if we've fallen out the bottom of the screen, we should remove the entity
                        // otherwise yield for a frame and loop
                        if (entity.Top <= level.Bounds.Bottom + 16 &&
                            (entity.Top <= level.Bounds.Bottom - 1 || !entity.CollideCheck<Solid>(entity.Position + Vector2.UnitY)))
                            yield return null;
                        else
                        {
                            // we've fallen out of the screen and should remove the entity
                            entity.Collidable = entity.Visible = false;
                            yield return 0.2f;
                            if (level.Session.MapData.CanTransitionTo(level, new Vector2(entity.Center.X, entity.Bottom + 12f)))
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
                level.DirectionalShake(Vector2.UnitY);
                entity.StartShaking();
                self.LandParticles?.Invoke();
                yield return 0.2f;
                entity.StopShaking();

                // if it's hit the fg tiles then make it safe and end
                if (entity.CollideCheck<SolidTiles>(entity.Position + Vector2.UnitY))
                {
                    entity.Safe |= self.ShouldManageSafe;
                    yield break;
                }

                // wait until we can fall again
                while (entity.CollideCheck<Platform>(entity.Position + Vector2.UnitY))
                    yield return 0.1f;
            }
        }
    }
}
