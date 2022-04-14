// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/BehaviorGravityController")]
    public class BehaviorGravityController : BaseGravityController
    {
        private const float default_holdable_reset_time = 2f;
        private const float default_spring_cooldown = 0.5f;

        private readonly float _holdableResetTime;
        private readonly float _springCooldown;

        protected new BehaviorGravityController CurrentChild => base.CurrentChild as BehaviorGravityController;

        public float HoldableResetTime => CurrentChild?.HoldableResetTime ?? _holdableResetTime;
        public float SpringCooldown => CurrentChild?.SpringCooldown ?? _springCooldown;

        public BehaviorGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _holdableResetTime = data.Float("holdableResetTime", default_holdable_reset_time).ClampLower(0f);
            _springCooldown = data.Float("springCooldown", default_spring_cooldown).ClampLower(0f);
        }
    }
}
