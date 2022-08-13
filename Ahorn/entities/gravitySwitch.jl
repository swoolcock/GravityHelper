# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravitySwitch

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

const default_cooldown = 1.0

@mapdef Entity "GravityHelper/GravitySwitch" GravitySwitch(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    gravityType::Integer=2,
    cooldown::Real=default_cooldown,
    switchOnHoldables::Bool=true
)

const placements = Ahorn.PlacementDict(
    "Gravity Switch (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySwitch,
        "point",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Switch (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySwitch,
        "point",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Switch (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySwitch,
        "point",
        Dict{String, Any}(
            "gravityType" => 2,
        )
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

Ahorn.editingOptions(entity::GravitySwitch) = Dict{String, Any}( "gravityType" => gravityTypes )

const normalSprite = "objects/GravityHelper/gravitySwitch/switch12"
const invertedSprite = "objects/GravityHelper/gravitySwitch/switch01"
const toggleSprite = "objects/GravityHelper/gravitySwitch/toggle01"

Ahorn.editingIgnored(entity::GravitySwitch, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravitySwitch)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(normalSprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravitySwitch, room::Maple.Room)
    gravityType = get(entity.data, "gravityType", 0)
    sprite = gravityType == 0 ? normalSprite : gravityType == 1 ? invertedSprite : toggleSprite
    return Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end