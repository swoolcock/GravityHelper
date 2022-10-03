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

        private const float flash_time = 1f;
        private bool _flash;

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

            if (ShieldTimeRemaining <= flash_time && Scene.OnInterval(0.1f))
                _flash = !_flash;
        }

        public override void Render()
        {
            if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
            if (!_flash)
            {
                var offset = GravityHelperModule.ShouldInvertPlayer ? 4f : -4f;
                Draw.Circle(player.Center.X, player.Center.Y + offset, 14, Color.White, 3);
            }
        }

        public void Activate(float time)
        {
            if (GravityHelperModule.PlayerComponent is not { } playerComponent) return;
            ShieldTimeRemaining = ShieldTotalTime = time;
            Active = Visible = true;
            playerComponent.Locked = true;
            _flash = false;
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
