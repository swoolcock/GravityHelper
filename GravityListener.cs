using System;
using System.Linq;
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

        public void GravityChanged(GravityType type)
        {
            if (Entity is Spikes spikes && (spikes.Direction == Spikes.Directions.Down || spikes.Direction == Spikes.Directions.Up))
            {
                var currentLedgeBlocker = spikes.Components.Get<LedgeBlocker>();
                var requiresLedgeBlocker = spikes.Direction == Spikes.Directions.Up && type == GravityType.Normal ||
                                           spikes.Direction == Spikes.Directions.Down && type == GravityType.Inverted;

                if (currentLedgeBlocker != null && !requiresLedgeBlocker)
                    spikes.Remove(currentLedgeBlocker);
                else if (currentLedgeBlocker == null && requiresLedgeBlocker)
                    spikes.Add(new LedgeBlocker());
            }
        }
    }
}