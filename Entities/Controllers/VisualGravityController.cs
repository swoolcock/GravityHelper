// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/VisualGravityController")]
    [Tracked]
    public class VisualGravityController : BaseGravityController<VisualGravityController>
    {
        public float FieldArrowOpacity { get; }
        public float FieldBackgroundOpacity { get; }
        public float FieldParticleOpacity { get; }
        public float LineMinAlpha { get; }
        public float LineMaxAlpha { get; }
        public float LineFlashTime { get; }

        public VisualGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            FieldArrowOpacity = data.Float("fieldArrowOpacity", GravityField.DEFAULT_ARROW_OPACITY).Clamp(0f, 1f);
            FieldBackgroundOpacity = data.Float("fieldBackgroundOpacity", GravityField.DEFAULT_FIELD_OPACITY).Clamp(0f, 1f);
            FieldParticleOpacity = data.Float("fieldParticleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY).Clamp(0f, 1f);
            LineMinAlpha = data.Float("lineMinAlpha", GravityLine.DEFAULT_MIN_ALPHA).Clamp(0f, 1f);
            LineMaxAlpha = data.Float("lineMaxAlpha", GravityLine.DEFAULT_MAX_ALPHA).Clamp(0f, 1f);
            LineFlashTime = data.Float("lineFlashTime", GravityLine.DEFAULT_FLASH_TIME).Clamp(0f, 1f);
        }
    }
}
