# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperVisualGravityController

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/VisualGravityController" VisualGravityController(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    persistent::Bool=true,
    fieldArrowOpacity::Real=0.5,
    fieldBackgroundOpacity::Real=0.15,
    fieldParticleOpacity::Real=0.5,
    fieldNormalColor::String="0000FF",
    fieldInvertedColor::String="FF0000",
    fieldToggleColor::String="800080",
    fieldArrowColor::String="FFFFFF",
    fieldParticleColor::String="FFFFFF",
    fieldFlashOnTrigger::Bool=true,
    lineMinAlpha::Real=0.45,
    lineMaxAlpha::Real=0.95,
    lineFlashTime::Real=0.35,
    lineColor::String="FFFFFF",
    lineThickness::Real=2.0
)

const placements = Ahorn.PlacementDict(
    "Visual Gravity Controller (Single Room) (GravityHelper)" => Ahorn.EntityPlacement(
        VisualGravityController,
        "point",
        Dict{String, Any}(
            "persistent" => false,
        )
    ),
    "Visual Gravity Controller (Persistent) (GravityHelper)" => Ahorn.EntityPlacement(
        VisualGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_field = "objects/GravityHelper/gravityController/field"

Ahorn.editingIgnored(entity::VisualGravityController, multiple::Bool=false) = String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::VisualGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::VisualGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_field, 0, 0)
end

end