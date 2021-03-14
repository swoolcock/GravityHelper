using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/GravityTrigger")]
    public class GravityTrigger : Trigger
    {
        public GravityType GravityType { get; }

        public GravityTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            GravityType = (GravityType)data.Int("gravityType");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);

            GravityHelperModule.Session.Gravity = GravityType;
        }
    }
}