using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        public GravityListener()
            : base(true, false)
        {
        }

        public override void EntityAwake() => GravityChanged(GravityHelperModule.Instance.Gravity);

        public void GravityChanged(GravityType type, float momentumMultiplier = 1f)
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
                    player.Speed.Y *= -momentumMultiplier;
                    player.DashDir.Y *= -1;
                    player.SetVarJumpTimer(0f);

                    invertHitbox(normalHitbox);
                    invertHitbox(normalHurtbox);
                    invertHitbox(duckHitbox);
                    invertHitbox(duckHurtbox);
                    invertHitbox(starFlyHitbox);
                    invertHitbox(starFlyHurtbox);

                    Vector2 normalLightOffset = new Vector2(0.0f, -8f);
                    Vector2 duckingLightOffset = new Vector2(0.0f, -3f);

                    player.SetNormalLightOffset(type == GravityType.Normal ? normalLightOffset : -normalLightOffset);
                    player.SetDuckingLightOffset(type == GravityType.Normal ? duckingLightOffset : -duckingLightOffset);

                    var starFlyBloom = player.GetStarFlyBloom();
                    if (starFlyBloom != null)
                        starFlyBloom.Y = Math.Abs(starFlyBloom.Y) * (type == GravityType.Inverted ? 1 : -1);
                }
            }
        }
    }
}
