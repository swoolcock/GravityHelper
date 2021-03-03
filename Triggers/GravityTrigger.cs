using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace GravityHelper.Triggers
{
    [CustomEntity("GravityHelper/GravityTrigger")]
    public class GravityTrigger : Trigger
    {
        private readonly GravityType gravityType;

        public GravityTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            gravityType = (GravityType)data.Int("gravityType");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);

            GravityHelperModule.Gravity = gravityType;
        }
    }
}