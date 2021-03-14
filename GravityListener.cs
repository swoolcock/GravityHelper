using System.Linq;
using System.Reflection;
using Celeste;
using Monocle;

namespace GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        private static readonly FieldInfo normalHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
        private static readonly FieldInfo normalHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
        private static readonly FieldInfo duckHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
        private static readonly FieldInfo duckHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
        private static readonly FieldInfo starFlyHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHitbox");
        private static readonly FieldInfo starFlyHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHurtbox");
        private static readonly FieldInfo varJumpTimerFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "varJumpTimer");

        public GravityListener()
            : base(true, false)
        {
        }

        public override void EntityAwake() => GravityChanged(GravityHelperModule.Session.Gravity);

        public void GravityChanged(GravityType type)
        {
            if (Entity is Spikes spikes && (spikes.Direction == Spikes.Directions.Down || spikes.Direction == Spikes.Directions.Up))
            {
                var ledgeBlocker = spikes.Components.Get<LedgeBlocker>();
                if (spikes.Direction == Spikes.Directions.Up)
                    ledgeBlocker.Blocking = type == GravityType.Normal;
                else if (spikes.Direction == Spikes.Directions.Down)
                    ledgeBlocker.Blocking = type == GravityType.Inverted;
            }
            else if (Entity is Player player)
            {
                void invertHitbox(Hitbox hitbox) => hitbox.Position.Y = -hitbox.Position.Y - hitbox.Height;

                var normalHitbox = (Hitbox) normalHitboxFieldInfo.GetValue(player);
                var collider = player.Collider ?? normalHitbox;

                if (type == GravityType.Inverted && collider.Top < -1 || type == GravityType.Normal && collider.Bottom > 1)
                {
                    var normalHurtbox = (Hitbox) normalHurtboxFieldInfo.GetValue(player);
                    var duckHitbox = (Hitbox) duckHitboxFieldInfo.GetValue(player);
                    var duckHurtbox = (Hitbox) duckHurtboxFieldInfo.GetValue(player);
                    var starFlyHitbox = (Hitbox) starFlyHitboxFieldInfo.GetValue(player);
                    var starFlyHurtbox = (Hitbox) starFlyHurtboxFieldInfo.GetValue(player);

                    player.Position.Y = type == GravityType.Inverted ? collider.AbsoluteTop : collider.AbsoluteBottom;
                    player.Speed.Y *= -1;
                    player.DashDir.Y *= -1;
                    varJumpTimerFieldInfo.SetValue(player, 0f);

                    invertHitbox(normalHitbox);
                    invertHitbox(normalHurtbox);
                    invertHitbox(duckHitbox);
                    invertHitbox(duckHurtbox);
                    invertHitbox(starFlyHitbox);
                    invertHitbox(starFlyHurtbox);
                }
            }
        }
    }
}