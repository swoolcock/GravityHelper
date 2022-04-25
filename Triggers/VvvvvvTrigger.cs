// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/VvvvvvTrigger")]
    public class VvvvvvTrigger : Trigger
    {
        public bool Enable { get; }
        public bool OnlyOnSpawn { get; }

        public VvvvvvTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            Enable = data.Bool("enable", true);
            OnlyOnSpawn = data.Bool("onlyOnSpawn");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);

            if (OnlyOnSpawn) return;

            GravityHelperModule.Session.VvvvvvTrigger = Enable;

            if (Scene.GetPersistentController<VvvvvvGravityController>() is { } vvvvvvGravityController)
                vvvvvvGravityController.Transitioned();
        }
    }
}
