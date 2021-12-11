module GravityHelperGravityBooster

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/GravityBooster" GravityBooster(
    x::Integer, y::Integer,
    gravityType::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Gravity Booster (Gravity Helper)" => Ahorn.EntityPlacement(
        GravityBooster
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    0 => (0.0, 0.0, 1.0, 0.5),
    1 => (1.0, 0.0, 0.0, 0.5),
    2 => (0.5, 0.0, 0.5, 0.5),
)

Ahorn.editingOptions(trigger::GravityBooster) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

sprite = "objects/booster/booster00"

function Ahorn.selection(entity::GravityBooster)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityBooster, room::Maple.Room)
    gravityType = get(entity.data, "gravityType", 0)
    Ahorn.drawSprite(ctx, sprite, 0, 0, tint=gravityColors[gravityType])
end

end