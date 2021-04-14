using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper.Triggers
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