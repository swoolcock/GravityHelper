# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityIndicator

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityIndicator" GravityIndicator(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    depth::Integer=8500,
    showRipples::Bool=false,
    showParticles::Bool=true,
    bloomAlpha::Real=0.6,
    bloomRadius::Real=14.0,
    idleAlpha::Real=1.0,
    turningAlpha::Real=0.4,
    turnTime::Real=0.3,
    syncToPlayer::Bool=false
)

const placements = Ahorn.PlacementDict(
    "Gravity Indicator (Cassette Controller) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityIndicator
    ),
    "Gravity Indicator (Player Synced) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityIndicator,
        "point",
        Dict{String, Any}(
            "syncToPlayer" => true,
        )
    ),
)

const sprite = "objects/GravityHelper/gravityIndicator/arrow00"

Ahorn.editingIgnored(entity::GravityIndicator, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravityIndicator)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityIndicator, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end