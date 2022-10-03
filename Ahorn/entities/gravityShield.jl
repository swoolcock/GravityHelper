# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityShield

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityShield" GravityShield(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    oneUse::Bool=false,
    shieldTime::Real=3,
    respawnTime::Real=2.5,
)

const placements = Ahorn.PlacementDict(
    "Gravity Shield (GravityHelper)" => Ahorn.EntityPlacement(
        GravityShield
    ),
)

const sprite = "objects/GravityHelper/gravityShield/idle00"

Ahorn.editingIgnored(entity::GravityShield, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravityShield)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 4, y - 5, 8, 10)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityShield, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end