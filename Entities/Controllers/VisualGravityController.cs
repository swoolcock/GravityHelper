// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
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
        private readonly float _lineMinAlpha;
        private readonly float _lineMaxAlpha;
        private readonly float _lineFlashTime;

        public float FieldArrowOpacity => CurrentChild?.FieldArrowOpacity ?? _fieldArrowOpacity;
        public float FieldBackgroundOpacity => CurrentChild?.FieldBackgroundOpacity ?? _fieldBackgroundOpacity;
        public float FieldParticleOpacity => CurrentChild?.FieldParticleOpacity ?? _fieldParticleOpacity;
        public float LineMinAlpha => CurrentChild?.LineMinAlpha ?? _lineMinAlpha;
        public float LineMaxAlpha => CurrentChild?.LineMaxAlpha ?? _lineMaxAlpha;
        public float LineFlashTime => CurrentChild?.LineFlashTime ?? _lineFlashTime;

        protected new VisualGravityController CurrentChild => base.CurrentChild as VisualGravityController;

        public VisualGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _fieldArrowOpacity = data.Float("fieldArrowOpacity", GravityField.DEFAULT_ARROW_OPACITY).Clamp(0f, 1f);
            _fieldBackgroundOpacity = data.Float("fieldBackgroundOpacity", GravityField.DEFAULT_FIELD_OPACITY).Clamp(0f, 1f);
            _fieldParticleOpacity = data.Float("fieldParticleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY).Clamp(0f, 1f);
            _lineMinAlpha = data.Float("lineMinAlpha", GravityLine.DEFAULT_MIN_ALPHA).Clamp(0f, 1f);
            _lineMaxAlpha = data.Float("lineMaxAlpha", GravityLine.DEFAULT_MAX_ALPHA).Clamp(0f, 1f);
            _lineFlashTime = data.Float("lineFlashTime", GravityLine.DEFAULT_FLASH_TIME).Clamp(0f, 1f);
        }
    }
}
