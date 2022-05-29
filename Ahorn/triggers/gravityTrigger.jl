# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityTrigger

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Trigger "GravityHelper/GravityTrigger" GravityTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    gravityType::Integer=0, momentumMultiplier::Real=1.0, sound::String="",
    affectsPlayer::Bool=true, affectsHoldableActors::Bool=false, affectsOtherActors::Bool=false
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const placements = Ahorn.PlacementDict(
    "Gravity Trigger (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityTrigger,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Trigger (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityTrigger,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Trigger (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityTrigger,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 2,
        )
    ),
)

Ahorn.editingIgnored(trigger::GravityTrigger, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(trigger::GravityTrigger) = Dict{String, Any}(
    "gravityType" => gravityTypes
)

end
