module GravityHelperFieldGravityController

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/FieldGravityController" FieldGravityController(
    x::Integer, y::Integer,
    persistent::Bool=false,
    arrowOpacity::Real=0.5,
    fieldOpacity::Real=0.15,
    particleOpacity::Real=0.5
)

const placements = Ahorn.PlacementDict(
    "Field Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        FieldGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_field = "objects/GravityHelper/gravityController/field"

function Ahorn.selection(entity::FieldGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FieldGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_field, 0, 0)
end

end