# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperVvvvvvTrigger

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Trigger "GravityHelper/VvvvvvTrigger" VvvvvvTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    pluginVersion::String=PLUGIN_VERSION,
    enable::Bool=true,
    onlyOnSpawn::Bool=false
)

const placements = Ahorn.PlacementDict(
    "VVVVVV Trigger (Enable) (GravityHelper)" => Ahorn.EntityPlacement(
        VvvvvvTrigger,
        "rectangle",
        Dict{String, Any}(
            "enable" => true,
        )
    ),
    "VVVVVV Trigger (Disable) (GravityHelper)" => Ahorn.EntityPlacement(
        VvvvvvTrigger,
        "rectangle",
        Dict{String, Any}(
            "enable" => false,
        )
    ),
)

Ahorn.editingIgnored(trigger::VvvvvvTrigger, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

end