// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBooster")]
    public class GravityBooster : Booster
    {
        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        public GravityType GravityType { get; }

        private readonly DynData<Booster> _data;

        public GravityBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, false)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            _data = new DynData<Booster>(this);
            GravityType = (GravityType)data.Int("gravityType");
        }

        public override void Render()
        {
            var sprite = _data.Get<Sprite>("sprite");
            var oldColor = sprite.Color;
            sprite.Color = GravityType.Color();
            base.Render();
            sprite.Color = oldColor;
        }
    }
}
