// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/VisualGravityController")]
    public class VisualGravityController : BaseGravityController
    {
        private readonly float _fieldArrowOpacity;
        private readonly float _fieldBackgroundOpacity;
        private readonly float _fieldParticleOpacity;

        public float FieldArrowOpacity => CurrentChild?.FieldArrowOpacity ?? _fieldArrowOpacity;
        public float FieldBackgroundOpacity => CurrentChild?.FieldBackgroundOpacity ?? _fieldBackgroundOpacity;
        public float FieldParticleOpacity => CurrentChild?.FieldParticleOpacity ?? _fieldParticleOpacity;

        protected new VisualGravityController CurrentChild => base.CurrentChild as VisualGravityController;

        public VisualGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _fieldArrowOpacity = Calc.Clamp(data.Float("fieldArrowOpacity", GravityField.DEFAULT_ARROW_OPACITY), 0f, 1f);
            _fieldBackgroundOpacity = Calc.Clamp(data.Float("fieldBackgroundOpacity", GravityField.DEFAULT_FIELD_OPACITY), 0f, 1f);
            _fieldParticleOpacity = Calc.Clamp(data.Float("fieldParticleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY), 0f, 1f);
        }
    }
}
