# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperForceLoadGravityController

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/ForceLoadGravityController" ForceLoadGravityController(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
)

const placements = Ahorn.PlacementDict(
    "Force Load Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        ForceLoadGravityController,
    ),
)

const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_bang = "objects/GravityHelper/gravityController/bang"

Ahorn.editingIgnored(entity::ForceLoadGravityController, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::ForceLoadGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite_dot, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ForceLoadGravityController)
    Ahorn.drawSprite(ctx, sprite_dot, 0, 0)
    Ahorn.drawSprite(ctx, sprite_bang, 0, 0)
end

end