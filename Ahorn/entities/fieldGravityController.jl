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

const sprite = "objects/GravityHelper/gravityController/icon"

function Ahorn.selection(entity::FieldGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FieldGravityController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end