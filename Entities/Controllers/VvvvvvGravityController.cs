// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/VvvvvvGravityController")]
    [Tracked]
    public class VvvvvvGravityController : BaseGravityController<VvvvvvGravityController>
    {
        public VvvvvvMode Mode { get; }
        public string FlipSound { get; }
        public bool DisableGrab { get; }
        public bool DisableDash { get; }

        public bool IsVvvvvv => Mode == VvvvvvMode.TriggerBased && GravityHelperModule.Session.VvvvvvTrigger || Mode == VvvvvvMode.On;

        private bool _dashDisabled;
        private float _bufferTimeRemaining;
        private bool _autoFlip;

        private const float flip_buffer_seconds = 0.1f;

        public VvvvvvGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            Mode = data.Enum("mode", VvvvvvMode.TriggerBased);
            FlipSound = data.Attr("flipSound", string.Empty);
            DisableGrab = data.Bool("disableGrab", true);
            DisableDash = data.Bool("disableDash", true);
        }

        public override void Transitioned()
        {
            if (!Persistent) return;

            var active = ActiveController;
            var session = GravityHelperModule.Session;
            session.DisableGrab = active.DisableGrab;
            session.VvvvvvMode = active.Mode;
        }

        public void CheckJump(Player player)
        {
            if (!Persistent) return;

            var active = ActiveController;
            if (!active.IsVvvvvv) return;

            // greedily consume all jumps
            var jumpPressed = Input.Jump.Pressed;
            Input.Jump.ConsumePress();

            // do nothing if we don't press jump
            if (!jumpPressed) return;

            if (canFlip(player, out var jumpBuffer))
            {
                // consume an Extended Variant Jump(TM)
                if (jumpBuffer > 0)
                    ReflectionCache.ExtendedVariantsJumpCountSetJumpCountMethodInfo?.Invoke(null, new object[] { jumpBuffer - 1, false });
                // do the flip right now
                doFlip(player);
            }
            else
            {
                // delay the flip until we're able to
                _autoFlip = true;
                _bufferTimeRemaining = flip_buffer_seconds;
            }
        }

        private bool canFlip(Player player, out int jumpBuffer)
        {
            // assume no extended variants jumps
            jumpBuffer = 0;

            // for now we'll only allow normal state
            if (player.StateMachine != Player.StNormal) return false;

            var playerData = new DynData<Player>(player);

            // never allow from dream jumps
            if (playerData.Get<bool>("dreamJump")) return false;

            // on ground or within coyote frames
            var onGround = player.OnGround() || playerData.Get<float>("jumpGraceTimer") > 0;

            // see if an Extended Variant Jump(TM) is available
            if (ReflectionCache.ExtendedVariantsJumpCountGetJumpBufferMethodInfo != null)
                jumpBuffer = (int)ReflectionCache.ExtendedVariantsJumpCountGetJumpBufferMethodInfo.Invoke(null, new object[0]);

            // we can flip if we're on the ground or have an extended variant jump
            return onGround || jumpBuffer > 0;
        }

        private void doFlip(Player player)
        {
            _autoFlip = false;
            _bufferTimeRemaining = 0f;

            var active = ActiveController;

            GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Toggle);
            player.Speed.Y = 160f * (player.SceneAs<Level>().InSpace ? 0.6f : 1f);
            if (!string.IsNullOrEmpty(active.FlipSound))
                Audio.Play(active.FlipSound);
        }

        public override void Update()
        {
            base.Update();
            if (!Persistent) return;

            var active = ActiveController;

            if (_dashDisabled != active.IsVvvvvv && active.DisableDash)
            {
                _dashDisabled = active.IsVvvvvv && active.DisableDash;
                updateInventory();
            }

            if (_bufferTimeRemaining > 0)
            {
                _bufferTimeRemaining -= Engine.DeltaTime;
                if (_autoFlip && Scene.Tracker.GetEntity<Player>() is { } player && canFlip(player, out _))
                    doFlip(player);
            }
        }

        private void updateInventory()
        {
            if (Scene is not Level level || level.Tracker.GetEntity<Player>() is not { } player)
                return;

            if (_dashDisabled)
            {
                level.Session.Inventory = PlayerInventory.Prologue;
                player.Dashes = 0;
            }
            else
            {
                // TODO: not force default
                level.Session.Inventory = PlayerInventory.Default;
            }
        }
    }
}
