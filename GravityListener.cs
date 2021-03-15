using Celeste;
using Monocle;

namespace GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
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

                var normalHitbox = player.GetNormalHitbox();
                var collider = player.Collider ?? normalHitbox;

                if (type == GravityType.Inverted && collider.Top < -1 || type == GravityType.Normal && collider.Bottom > 1)
                {
                    var normalHurtbox = player.GetNormalHurtbox();
                    var duckHitbox = player.GetDuckHitbox();
                    var duckHurtbox = player.GetDuckHurtbox();
                    var starFlyHitbox = player.GetStarFlyHitbox();
                    var starFlyHurtbox = player.GetStarFlyHurtbox();

                    player.Position.Y = type == GravityType.Inverted ? collider.AbsoluteTop : collider.AbsoluteBottom;
                    player.Speed.Y *= -1;
                    player.DashDir.Y *= -1;
                    player.SetVarJumpTimer(0f);

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