module GravityHelperBehaviorGravityController

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/BehaviorGravityController" BehaviorGravityController(
    x::Integer, y::Integer,
    persistent::Bool=true,
    holdableResetTime::Real=2.0,
    springCooldown::Real=1.0
)

const placements = Ahorn.PlacementDict(
    "Behavior Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        BehaviorGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_field = "objects/GravityHelper/gravityController/field"

function Ahorn.selection(entity::BehaviorGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BehaviorGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_field, 0, 0)
end

end