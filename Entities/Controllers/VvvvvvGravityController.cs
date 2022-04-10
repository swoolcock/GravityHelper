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

            UpdateInventory();
        }

        public void UpdateInventory()
        {
            if (Scene is not Level level || level.Tracker.GetEntity<Player>() is not { } player)
                return;

            if (IsVvvvvv && DisableDash)
            {
                level.Session.Inventory = PlayerInventory.Prologue;
                player.Dashes = 0;
            }
            else if (level.Session.Inventory.Dashes == 0)
            {
                // Logger.Log(nameof(GravityHelperModule), $"inventory: {level.Session.MapData.Meta.Inventory}");
                // var startInventoryString = level.Session.MapData.Meta.Inventory;
                // var startInventoryField = typeof(PlayerInventory).GetField(startInventoryString, BindingFlags.Public | BindingFlags.Static);
                // var startInventory = (PlayerInventory)startInventoryField.GetValue(null);
                // level.Session.Inventory = startInventory;
                // TODO: not force default
                level.Session.Inventory = PlayerInventory.Default;
            }
        }
    }
}
