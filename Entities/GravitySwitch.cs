// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravitySwitch")]
    public class GravitySwitch : Entity
    {
        public float Cooldown { get; private set; }
        public GravityType GravityType { get; }
        public bool SwitchOnHoldables { get; private set; }

        private readonly bool _defaultToController;
        private readonly Sprite _sprite;

        private float _cooldownRemaining;
        private bool _playSounds;

        private bool usable => GravityType != (GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

        public GravitySwitch(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            GravityType = data.Enum("gravityType", GravityType.Toggle);
            Cooldown = data.Float("cooldown", BehaviorGravityController.DEFAULT_SWITCH_COOLDOWN);
            SwitchOnHoldables = data.Bool("switchOnHoldables", true);

            _defaultToController = data.Bool("defaultToController", true);

            Collider = new Hitbox(16f, 24f, -8f, -12f);

            Add(new HoldableCollider(OnHoldable));
            Add(new PlayerCollider(OnPlayer));
            Add(new PlayerGravityListener(OnGravityChanged));
            Add(_sprite = GFX.SpriteBank.Create(GravityType == GravityType.Toggle ? "gravitySwitchToggle" : "gravitySwitch"));

            Depth = Depths.Below;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (_defaultToController && Scene.GetActiveController<BehaviorGravityController>() is { } behaviorController)
            {
                Cooldown = behaviorController.SwitchCooldown;
                SwitchOnHoldables = behaviorController.SwitchOnHoldables;
            }

            updateSprite(false);
        }

        private void updateSprite(bool animate)
        {
            var currentGravity = GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal;
            var up = currentGravity == GravityType.Inverted;
            var key = up ? "up" : "down";

            if (animate)
            {
                if (_playSounds)
                    Audio.Play(up ? "event:/game/09_core/switch_to_cold" : "event:/game/09_core/switch_to_hot", Position);
                if (usable)
                    _sprite.Play(key);
                else
                {
                    if (_playSounds)
                        Audio.Play("event:/game/09_core/switch_dies", Position);
                    _sprite.Play($"{key}Off");
                }
            }
            else if (usable)
                _sprite.Play($"{key}Loop");
            else
                _sprite.Play($"{key}OffLoop");

            _playSounds = false;
        }

        private void OnHoldable(Holdable holdable)
        {
            if (SwitchOnHoldables && !holdable.IsHeld)
                trigger(holdable.Entity);
        }

        private void OnPlayer(Player player) => trigger(player);

        private void trigger(Entity entity)
        {
            if (!usable || _cooldownRemaining > 0)
                return;

            _playSounds = true;
            _cooldownRemaining = Cooldown;

            GravityHelperModule.PlayerComponent?.SetGravity(GravityType);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            SceneAs<Level>().Flash(Color.White * 0.15f, true);
            Celeste.Freeze(0.05f);
        }

        private void OnGravityChanged(Entity entity, GravityChangeArgs args) => updateSprite(args.Changed);

        public override void Update()
        {
            base.Update();
            if (_cooldownRemaining > 0)
                _cooldownRemaining -= Engine.DeltaTime;
        }
    }
}
