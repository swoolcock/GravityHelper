// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/FieldGravityController")]
    public class FieldGravityController : BaseGravityController
    {
        private readonly float _arrowOpacity;
        private readonly float _fieldOpacity;
        private readonly float _particleOpacity;

        public float ArrowOpacity => CurrentChild?.ArrowOpacity ?? _arrowOpacity;
        public float FieldOpacity => CurrentChild?.FieldOpacity ?? _fieldOpacity;
        public float ParticleOpacity => CurrentChild?.ParticleOpacity ?? _particleOpacity;

        protected new FieldGravityController CurrentChild => base.CurrentChild as FieldGravityController;

        public FieldGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _arrowOpacity = Calc.Clamp(data.Float("arrowOpacity", GravityField.DEFAULT_ARROW_OPACITY), 0f, 1f);
            _fieldOpacity = Calc.Clamp(data.Float("fieldOpacity", GravityField.DEFAULT_FIELD_OPACITY), 0f, 1f);
            _particleOpacity = Calc.Clamp(data.Float("particleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY), 0f, 1f);
        }
    }
}
