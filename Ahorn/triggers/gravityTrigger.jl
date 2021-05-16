module GravityHelperGravityTrigger

using ..Ahorn, Maple

@mapdef Trigger "GravityHelper/GravityTrigger" GravityTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    gravityType::Integer=0, momentumMultiplier::Real=1.0
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const placements = Ahorn.PlacementDict(
    "Gravity Trigger (GravityHelper)" => Ahorn.EntityPlacement(
        GravityTrigger,
        "rectangle",
    ),
)

Ahorn.editingOptions(trigger::GravityTrigger) = Dict{String, Any}(
    "gravityType" => gravityTypes
)

end
