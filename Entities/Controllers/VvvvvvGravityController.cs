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

            var playerData = new DynData<Player>(player);
            var jumpPressed = Input.Jump.Pressed;
            Input.Jump.ConsumePress();

            if (jumpPressed && (player.OnGround() || playerData.Get<float>("jumpGraceTimer") > 0))
            {
                GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Toggle);
                player.Speed.Y = 160f * (player.SceneAs<Level>().InSpace ? 0.6f : 1f);
                if (!string.IsNullOrEmpty(active.FlipSound))
                    Audio.Play(active.FlipSound);
            }
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
