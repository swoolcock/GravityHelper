# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperInversionBlock

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/InversionBlock" InversionBlock(
    x::Integer, y::Integer,
    width::Integer=32, height::Integer=32,
    leftGravityType::Integer=2, rightGravityType::Integer=2,
    topEnabled::Bool=true, bottomEnabled::Bool=true, leftEnabled::Bool=false, rightEnabled::Bool=false,
    pluginVersion::String=PLUGIN_VERSION,
)

const placements = Ahorn.PlacementDict(
    "Inversion Block (GravityHelper)" => Ahorn.EntityPlacement(
        InversionBlock,
        "rectangle",
        Dict{String, Any}(
            "topEnabled" => true,
            "bottomEnabled" => true,
            "leftEnabled" => false,
            "rightEnabled" => false,
        ),
    ),
    "Inversion Block (Toggle Sides) (GravityHelper)" => Ahorn.EntityPlacement(
        InversionBlock,
        "rectangle",
        Dict{String, Any}(
            "topEnabled" => false,
            "bottomEnabled" => false,
            "leftEnabled" => true,
            "rightEnabled" => true,
        ),
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

Ahorn.editingIgnored(entity::InversionBlock, multiple::Bool=false) = multiple ? String["x", "y", "width", "height", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]
Ahorn.editingOptions(entity::InversionBlock) = Dict{String, Any}( "leftGravityType" => gravityTypes, "rightGravityType" => gravityTypes )
Ahorn.minimumSize(entity::InversionBlock) = 8, 8
Ahorn.resizable(entity::InversionBlock) = true, true

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::InversionBlock, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    Ahorn.drawRectangle(ctx, 0, 0, width, height, (1.0, 1.0, 1.0, 0.2), (1.0, 1.0, 1.0, 1.0))
end

end