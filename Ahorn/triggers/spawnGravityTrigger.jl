module GravityHelperSpawnGravityTrigger

using ..Ahorn, Maple

@mapdef Trigger "GravityHelper/SpawnGravityTrigger" SpawnGravityTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    gravityType::Integer=0, fireOnBubbleReturn::Bool=true
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const placements = Ahorn.PlacementDict(
    "Spawn Gravity Trigger (GravityHelper)" => Ahorn.EntityPlacement(
        SpawnGravityTrigger,
        "rectangle",
    ),
)

Ahorn.editingOptions(trigger::SpawnGravityTrigger) = Dict{String, Any}(
    "gravityType" => gravityTypes
)

end