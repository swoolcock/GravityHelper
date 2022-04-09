// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/FieldGravityController")]
    [Tracked]
    public class FieldGravityController : BaseGravityController
    {
        public float ArrowOpacity { get; }
        public float FieldOpacity { get; }
        public float ParticleOpacity { get; }

        public FieldGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            ArrowOpacity = Calc.Clamp(data.Float("arrowOpacity", GravityField.DEFAULT_ARROW_OPACITY), 0f, 1f);
            FieldOpacity = Calc.Clamp(data.Float("fieldOpacity", GravityField.DEFAULT_FIELD_OPACITY), 0f, 1f);
            ParticleOpacity = Calc.Clamp(data.Float("particleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY), 0f, 1f);
        }
    }
}
