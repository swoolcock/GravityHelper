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
        private const float default_holdable_reset_time = 2f;
        private const float default_spring_cooldown = 0.5f;

        public float HoldableResetTime { get; }
        public float SpringCooldown { get; }

        public BehaviorGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            HoldableResetTime = data.Float("holdableResetTime", default_holdable_reset_time).ClampLower(0f);
            SpringCooldown = data.Float("springCooldown", default_spring_cooldown).ClampLower(0f);
        }
    }
}
