// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

internal class PlayerGravityComponent : GravityComponent
{
    public static readonly string PLAYER_FLAG = "GravityHelper_PlayerInverted";

    public GravityType PreDreamBlockGravityType { get; set; }
    public new Player Entity => base.Entity as Player;

    public PlayerGravityComponent()
    {
        CheckInvert = () =>
            Entity.StateMachine.State switch
            {
                Player.StDreamDash => false,
                Player.StAttract => false,
                Player.StDummy when !Entity.DummyGravity => false,
                Player.StBoost => false,
                Player.StIntroWalk => false,
                Player.StIntroRespawn => false,
                Player.StIntroWakeUp => false,
                // Player.StIntroJump is set during a summit launch up transition, so we can't ignore it
                _ => true,
            };

        UpdateVisuals = args =>
        {
            if (!args.Changed || Entity.Scene == null) return;

            Vector2 normalLightOffset = new Vector2(0.0f, args.NewValue == GravityType.Normal ? -8f : 8f);
            Vector2 duckingLightOffset = new Vector2(0.0f, args.NewValue == GravityType.Normal ? -3f : 3f);

            Entity.normalLightOffset = normalLightOffset;
            Entity.duckingLightOffset = duckingLightOffset;
            Entity.Light.Position = Entity.Ducking ? duckingLightOffset : normalLightOffset;

            var starFlyBloom = Entity.starFlyBloom;
            if (starFlyBloom != null)
                starFlyBloom.Y = Math.Abs(starFlyBloom.Y) * (args.NewValue == GravityType.Inverted ? 1 : -1);
        };

        UpdatePosition = args =>
        {
            if (!args.Changed || Entity.Scene == null) return;

            var collider = Entity.Collider ?? Entity.normalHitbox;
            Entity.Position.Y = args.NewValue == GravityType.Inverted
                ? collider.AbsoluteTop
                : collider.AbsoluteBottom;
        };

        UpdateColliders = args =>
        {
            if (!args.Changed || Entity.Scene == null) return;

            invertHitbox(Entity.normalHitbox);
            invertHitbox(Entity.normalHurtbox);
            invertHitbox(Entity.duckHitbox);
            invertHitbox(Entity.duckHurtbox);
            invertHitbox(Entity.starFlyHitbox);
            invertHitbox(Entity.starFlyHurtbox);
        };

        UpdateSpeed = args =>
        {
            if (!args.Changed || Entity.Scene == null) return;

            if (args.Instant)
                Entity.EnsureFallingSpeed();
            else
                Entity.Speed.Y *= -args.MomentumMultiplier;

            Entity.DashDir.Y *= -1;
            Entity.varJumpTimer = 0f;

            // update player on ground status
            checkGround(Entity, args.NewValue, out var onGround, out var onSafeGround);

            var oldOnGround = Entity.onGround;
            if (oldOnGround && !onGround)
                Entity.jumpGraceTimer = 0f;
            else if (!oldOnGround && onGround)
                Entity.StartJumpGraceTime();

            Entity.onGround = onGround;
            Entity.OnSafeGround = onSafeGround;
        };

        GetSpeed = () => Entity.Speed;
        SetSpeed = value => Entity.Speed = value;

        Flag = PLAYER_FLAG;
    }

    private static void invertHitbox(Hitbox hitbox) => hitbox.Position.Y = -hitbox.Position.Y - hitbox.Height;

    private static void checkGround(Player self, GravityType type, out bool onGround, out bool onSafeGround)
    {
        var direction = type == GravityType.Inverted ? -Vector2.UnitY : Vector2.UnitY;

        if (self.StateMachine.State == Player.StDreamDash)
            onGround = onSafeGround = false;
        else if (self.Speed.Y >= 0f)
        {
            var platform = (Platform) self.CollideFirst<Solid>(self.Position + direction) ??
                (type == GravityType.Inverted
                    ? self.CollideFirstOutsideUpsideDownJumpThru(self.Position + direction)
                    : self.CollideFirstOutsideNotUpsideDownJumpThru(self.Position + direction));
            if (platform != null)
            {
                onGround = true;
                onSafeGround = platform.Safe;
            }
            else
                onGround = onSafeGround = false;
        }
        else
            onGround = onSafeGround = false;

        if (self.StateMachine.State == Player.StSwim)
            onSafeGround = true;

        if (onSafeGround)
        {
            foreach (var component in self.Scene.Tracker.GetComponents<SafeGroundBlocker>())
            {
                if (component is SafeGroundBlocker safeGroundBlocker && safeGroundBlocker.Check(self))
                {
                    onSafeGround = false;
                    break;
                }
            }
        }
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);

        // cache when the component is added so that it's available before the player is added to the scene
        GravityHelperModule.PlayerComponent = this;
    }

    public override void Removed(Entity entity)
    {
        base.Removed(entity);

        // only clear the cache if it's ourselves (note that this may never be called)
        if (GravityHelperModule.PlayerComponent == this)
            GravityHelperModule.PlayerComponent = null;
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);

        // cache when the player is added to the scene in case it was removed and re-added
        GravityHelperModule.PlayerComponent = this;
    }

    public override void EntityRemoved(Scene scene)
    {
        base.EntityRemoved(scene);

        // only clear the cache if it's ourselves
        if (GravityHelperModule.PlayerComponent == this)
            GravityHelperModule.PlayerComponent = null;
    }
}
