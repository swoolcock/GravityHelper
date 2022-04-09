// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/VvvvvvGravityController")]
    [Tracked]
    public class VvvvvvGravityController : BaseGravityController
    {
        public VvvvvvMode Mode { get; }
        public string FlipSound { get; }
        public bool DisableGrab { get; }
        public bool DisableDash { get; }

        public VvvvvvGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            Mode = data.Enum("mode", VvvvvvMode.TriggerBased);
            FlipSound = data.Attr("flipSound", string.Empty);
            DisableGrab = data.Bool("disableGrab", true);
            DisableDash = data.Bool("disableDash", true);
        }

        protected override void Apply()
        {
            GravityHelperModule.Session.VvvvvvMode = Mode;
            GravityHelperModule.Session.VvvvvvFlipSound = FlipSound;
            GravityHelperModule.Session.VvvvvvDisableGrab = DisableGrab;
            GravityHelperModule.Session.VvvvvvDisableDash = DisableDash;

            UpdateInventory();
        }

        public static void UpdateInventory()
        {
            if (Engine.Scene is not Level level || level.Tracker.GetEntity<Player>() is not { } player)
                return;

            if (GravityHelperModule.Session.IsVvvvvv && GravityHelperModule.Session.VvvvvvDisableDash)
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
