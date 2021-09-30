// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/SpawnGravityTrigger")]
    [Tracked]
    public class SpawnGravityTrigger : Entity
    {
        public GravityType GravityType { get; }
        public bool FireOnBubbleReturn { get; }

        public SpawnGravityTrigger(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            GravityType = (GravityType)data.Int("gravityType");
            FireOnBubbleReturn = data.Bool("fireOnBubbleReturn", true);
            Collider = new Hitbox(data.Width, data.Height);
            Visible = Active = false;
        }
    }
}
