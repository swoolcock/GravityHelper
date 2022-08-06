// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Components
{
    public class PlayerGravityComponent : GravityComponent
    {
        public GravityType PreDreamBlockGravityType { get; set; }
        private Player _player => Entity as Player;
        private DynData<Player> _playerData;

        public PlayerGravityComponent()
        {
            CheckInvert = () =>
                _player.StateMachine.State switch
                {
                    Player.StDreamDash => false,
                    Player.StAttract => false,
                    Player.StDummy when !_player.DummyGravity => false,
                    _ when _player.CurrentBooster != null => false,
                    _ => true,
                };

            UpdateVisuals = args =>
            {
                if (!args.Changed || _player.Scene == null) return;

                Vector2 normalLightOffset = new Vector2(0.0f, -8f);
                Vector2 duckingLightOffset = new Vector2(0.0f, -3f);

                _playerData["normalLightOffset"] = args.NewValue == GravityType.Normal ? normalLightOffset : -normalLightOffset;
                _playerData["duckingLightOffset"] = args.NewValue == GravityType.Normal ? duckingLightOffset : -duckingLightOffset;
                _player.Light.Position = _player.Ducking ? duckingLightOffset : normalLightOffset;

                var starFlyBloom = _playerData.Get<BloomPoint>("starFlyBloom");
                if (starFlyBloom != null)
                    starFlyBloom.Y = Math.Abs(starFlyBloom.Y) * (args.NewValue == GravityType.Inverted ? 1 : -1);
            };

            UpdatePosition = args =>
            {
                if (!args.Changed || _player.Scene == null) return;

                var collider = _player.Collider ?? _playerData.Get<Hitbox>("normalHitbox");
                _player.Position.Y = args.NewValue == GravityType.Inverted
                    ? collider.AbsoluteTop
                    : collider.AbsoluteBottom;
            };

            UpdateColliders = args =>
            {
                if (!args.Changed || _player.Scene == null) return;

                invertHitbox(_playerData.Get<Hitbox>("normalHitbox"));
                invertHitbox(_playerData.Get<Hitbox>("normalHurtbox"));
                invertHitbox(_playerData.Get<Hitbox>("duckHitbox"));
                invertHitbox(_playerData.Get<Hitbox>("duckHurtbox"));
                invertHitbox(_playerData.Get<Hitbox>("starFlyHitbox"));
                invertHitbox(_playerData.Get<Hitbox>("starFlyHurtbox"));
            };

            UpdateSpeed = args =>
            {
                if (!args.Changed || _player.Scene == null) return;

                _player.Speed.Y *= -args.MomentumMultiplier;
                _player.DashDir.Y *= -1;
                _playerData["varJumpTimer"] = 0f;

                // update player on ground status
                checkGround(_player, args.NewValue, out var onGround, out var onSafeGround);

                var oldOnGround = _playerData.Get<bool>("onGround");
                if (oldOnGround && !onGround)
                    _playerData["jumpGraceTimer"] = 0f;
                else if (!oldOnGround && onGround)
                    _player.StartJumpGraceTime();

                _playerData["onGround"] = onGround;
                _player.SetOnSafeGround(onSafeGround);
            };

            GetSpeed = () => _player.Speed;
            SetSpeed = value => _player.Speed = value;
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

            _playerData = new DynData<Player>((Player) entity);
            GravityHelperModule.PlayerComponent = this;
        }

        public override void Removed(Entity entity)
        {
            base.Removed(entity);

            _playerData = null;
            GravityHelperModule.PlayerComponent = null;
        }
    }
}
