// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [Tracked]
    public class GravityShieldIndicator : Entity
    {
        public float ShieldTotalTime { get; set; }
        public float ShieldTimeRemaining { get; set; }

        public GravityShieldIndicator()
        {
            Depth = Depths.Top;
            Tag = Tags.Persistent;
            Visible = false;
            Active = false;
        }

        public override void Update()
        {
            base.Update();

            if (ShieldTimeRemaining > 0)
            {
                ShieldTimeRemaining -= Engine.DeltaTime;
                if (ShieldTimeRemaining <= 0)
                    Deactivate();
            }
        }

        public override void Render()
        {
            if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
            Draw.Circle(player.Center, 16, Color.White, 2);
        }

        public void Activate(float time)
        {
            if (GravityHelperModule.PlayerComponent is not { } playerComponent) return;
            ShieldTimeRemaining = ShieldTotalTime = time;
            Active = Visible = true;
            playerComponent.Locked = true;
        }

        public void Deactivate()
        {
            ShieldTimeRemaining = ShieldTotalTime = 0;
            Active = Visible = false;
            if (GravityHelperModule.PlayerComponent is { } playerComponent)
                playerComponent.Locked = false;
        }
    }
}
