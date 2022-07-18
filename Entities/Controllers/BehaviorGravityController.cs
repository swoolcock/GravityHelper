// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/BehaviorGravityController")]
    [Tracked]
    public class BehaviorGravityController : BaseGravityController<BehaviorGravityController>
    {
        public const float DEFAULT_HOLDABLE_RESET_TIME = 2f;
        public const float DEFAULT_SPRING_COOLDOWN = 0.5f;
        public const float DEFAULT_SWITCH_COOLDOWN = 1f;

        public float HoldableResetTime { get; } = DEFAULT_HOLDABLE_RESET_TIME;
        public float SpringCooldown { get; } = DEFAULT_SPRING_COOLDOWN;
        public float SwitchCooldown { get; } = DEFAULT_SWITCH_COOLDOWN;
        public bool SwitchOnHoldables { get; } = true;
        public bool DashToToggle { get; }

        // ReSharper disable once UnusedMember.Global
        public BehaviorGravityController()
        {
            // ephemeral controller
        }

        // ReSharper disable once UnusedMember.Global
        public BehaviorGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            HoldableResetTime = data.Float("holdableResetTime", DEFAULT_HOLDABLE_RESET_TIME).ClampLower(0f);
            SpringCooldown = data.Float("springCooldown", DEFAULT_SPRING_COOLDOWN).ClampLower(0f);
            SwitchCooldown = data.Float("switchCooldown", DEFAULT_SWITCH_COOLDOWN).ClampLower(0f);
            SwitchOnHoldables = data.Bool("switchOnHoldables", true);
            DashToToggle = data.Bool("dashToToggle");
        }
    }
}
