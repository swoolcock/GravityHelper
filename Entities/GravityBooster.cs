// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBooster")]
    public class GravityBooster : Booster
    {
        public GravityType GravityType { get; }

        private readonly DynData<Booster> _data;

        public GravityBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, false)
        {
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
