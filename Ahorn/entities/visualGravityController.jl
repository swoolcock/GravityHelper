module GravityHelperVisualGravityController

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/VisualGravityController" VisualGravityController(
    x::Integer, y::Integer,
    persistent::Bool=true,
    fieldArrowOpacity::Real=0.5,
    fieldBackgroundOpacity::Real=0.15,
    fieldParticleOpacity::Real=0.5,
    lineMinAlpha::Real=0.45,
    lineMaxAlpha::Real=0.95,
    lineFlashTime::Real=0.35
)

const placements = Ahorn.PlacementDict(
    "Visual Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        VisualGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_field = "objects/GravityHelper/gravityController/field"

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