// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
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

        // ReSharper disable NotAccessedField.Local
        private readonly VersionInfo _modVersion;
        private readonly VersionInfo _pluginVersion;
        // ReSharper restore NotAccessedField.Local

        public SpawnGravityTrigger(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");
            FireOnBubbleReturn = data.Bool("fireOnBubbleReturn", true);
            Collider = new Hitbox(data.Width, data.Height);
            Visible = Active = false;
        }
    }
}
