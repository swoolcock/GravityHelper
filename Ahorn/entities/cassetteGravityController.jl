# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperCassetteGravityController

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/CassetteGravityController" CassetteGravityController(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    persistent::Bool=true,
    cassetteSequence::String="0,1",
    momentumMultiplier::Real=1.0
)

const placements = Ahorn.PlacementDict(
    "Cassette Gravity Controller (Single Room) (GravityHelper)" => Ahorn.EntityPlacement(
        CassetteGravityController,
        "point",
        Dict{String, Any}(
            "persistent" => false,
        )
    ),
    "Cassette Gravity Controller (Persistent) (GravityHelper)" => Ahorn.EntityPlacement(
        CassetteGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_cassette = "objects/GravityHelper/gravityController/cassette"

Ahorn.editingIgnored(entity::CassetteGravityController, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::CassetteGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_cassette, 0, 0)
end

end