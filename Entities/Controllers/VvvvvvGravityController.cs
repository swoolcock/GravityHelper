// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/VvvvvvGravityController")]
    public class VvvvvvGravityController : BaseGravityController
    {
        public static bool DisableGrabCache;
        public static VvvvvvMode ModeCache;

        private readonly VvvvvvMode _mode;
        private readonly string _flipSound;
        private readonly bool _disableGrab;
        private readonly bool _disableDash;

        protected new VvvvvvGravityController CurrentChild => base.CurrentChild as VvvvvvGravityController;

        public VvvvvvMode Mode => CurrentChild?.Mode ?? _mode;
        public string FlipSound => CurrentChild?.FlipSound ?? _flipSound;
        public bool DisableGrab => CurrentChild?.DisableGrab ?? _disableGrab;
        public bool DisableDash => CurrentChild?.DisableDash ?? _disableDash;

        public bool IsVvvvvv => Mode == VvvvvvMode.TriggerBased && GravityHelperModule.Session.VvvvvvTrigger || Mode == VvvvvvMode.On;

        private bool _dashDisabled;

        public VvvvvvGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _mode = data.Enum("mode", VvvvvvMode.TriggerBased);
            _flipSound = data.Attr("flipSound", string.Empty);
            _disableGrab = data.Bool("disableGrab", true);
            _disableDash = data.Bool("disableDash", true);
        }

        public override void Apply()
        {
            DisableGrabCache = DisableGrab;
            ModeCache = Mode;
        }

        public override void Update()
        {
            base.Update();

            if (Persistent && _dashDisabled != IsVvvvvv && DisableDash)
            {
                _dashDisabled = IsVvvvvv && DisableDash;
                UpdateInventory();
            }
        }

        public void UpdateInventory()
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
