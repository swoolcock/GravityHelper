# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperSpawnGravityTrigger

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Trigger "GravityHelper/SpawnGravityTrigger" SpawnGravityTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    pluginVersion::String=PLUGIN_VERSION,
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

Ahorn.editingIgnored(trigger::SpawnGravityTrigger, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(trigger::SpawnGravityTrigger) = Dict{String, Any}(
    "gravityType" => gravityTypes
)

end