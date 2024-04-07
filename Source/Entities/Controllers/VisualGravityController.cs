// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers;

[CustomEntity("GravityHelper/VisualGravityController")]
[Tracked]
public class VisualGravityController : BaseGravityController<VisualGravityController>
{
    public float FieldArrowOpacity { get; } = GravityField.DEFAULT_ARROW_OPACITY;
    public float FieldBackgroundOpacity { get; } = GravityField.DEFAULT_FIELD_OPACITY;
    public float FieldParticleOpacity { get; } = GravityField.DEFAULT_PARTICLE_OPACITY;
    public string FieldNormalColor { get; } = "0000FF";
    public string FieldInvertedColor { get; } = "FF0000";
    public string FieldToggleColor { get; } = "800080";
    public string FieldArrowColor { get; } = GravityField.DEFAULT_ARROW_COLOR;
    public string FieldParticleColor { get; } = GravityField.DEFAULT_PARTICLE_COLOR;
    public bool FieldFlashOnTrigger { get; } = true;
    public bool FieldShowParticles { get; } = true;
    public int FieldParticleDensity { get; } = GravityField.DEFAULT_PARTICLE_DENSITY;
    public float LineMinAlpha { get; } = GravityLine.DEFAULT_MIN_ALPHA;
    public float LineMaxAlpha { get; } = GravityLine.DEFAULT_MAX_ALPHA;
    public float LineFlashTime { get; } = GravityLine.DEFAULT_FLASH_TIME;
    public string LineColor { get; } = GravityLine.DEFAULT_LINE_COLOR;
    public float LineThickness { get; } = GravityLine.DEFAULT_LINE_THICKNESS;

    // ReSharper disable once UnusedMember.Global
    public VisualGravityController()
    {
        // ephemeral controller
    }

    // ReSharper disable once UnusedMember.Global
    public VisualGravityController(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        FieldArrowOpacity = data.Float("fieldArrowOpacity", FieldArrowOpacity);
        FieldBackgroundOpacity = data.Float("fieldBackgroundOpacity", FieldBackgroundOpacity);
        FieldParticleOpacity = data.Float("fieldParticleOpacity", FieldParticleOpacity);
        FieldNormalColor = data.Attr("fieldNormalColor", FieldNormalColor);
        FieldInvertedColor = data.Attr("fieldInvertedColor", FieldInvertedColor);
        FieldToggleColor = data.Attr("fieldToggleColor", FieldToggleColor);
        FieldArrowColor = data.Attr("fieldArrowColor", FieldArrowColor);
        FieldParticleColor = data.Attr("fieldParticleColor", FieldParticleColor);
        FieldFlashOnTrigger = data.Bool("fieldFlashOnTrigger", FieldFlashOnTrigger);
        FieldShowParticles = data.Bool("fieldShowParticles",FieldShowParticles);
        FieldParticleDensity = data.Int("fieldParticleDensity", FieldParticleDensity);
        LineMinAlpha = data.Float("lineMinAlpha", LineMinAlpha);
        LineMaxAlpha = data.Float("lineMaxAlpha", LineMaxAlpha);
        LineFlashTime = data.Float("lineFlashTime", LineFlashTime);
        LineColor = data.Attr("lineColor", LineColor);
        LineThickness = data.Float("lineThickness", LineThickness);
    }
}
