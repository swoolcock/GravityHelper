// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
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
        public string FieldNormalColor { get; }
        public string FieldInvertedColor { get; }
        public string FieldToggleColor { get; }
        public string FieldArrowColor { get; }
        public string FieldParticleColor { get; }
        public bool FieldFlashOnTrigger { get; }
        public float LineMinAlpha { get; }
        public float LineMaxAlpha { get; }
        public float LineFlashTime { get; }
        public string LineColor { get; }

        public VisualGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            FieldArrowOpacity = data.Float("fieldArrowOpacity");
            FieldBackgroundOpacity = data.Float("fieldBackgroundOpacity");
            FieldParticleOpacity = data.Float("fieldParticleOpacity");
            FieldNormalColor = data.Attr("fieldNormalColor");
            FieldInvertedColor = data.Attr("fieldInvertedColor");
            FieldToggleColor = data.Attr("fieldToggleColor");
            FieldArrowColor = data.Attr("fieldArrowColor");
            FieldParticleColor = data.Attr("fieldParticleColor");
            FieldFlashOnTrigger = data.Bool("fieldFlashOnTrigger", true);
            LineMinAlpha = data.Float("lineMinAlpha");
            LineMaxAlpha = data.Float("lineMaxAlpha");
            LineFlashTime = data.Float("lineFlashTime");
            LineColor = data.Attr("lineColor");
        }
    }
}
