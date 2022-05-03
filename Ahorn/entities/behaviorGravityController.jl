# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperBehaviorGravityController

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/BehaviorGravityController" BehaviorGravityController(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    persistent::Bool=true,
    holdableResetTime::Real=2.0,
    springCooldown::Real=0.1
)

const placements = Ahorn.PlacementDict(
    "Behavior Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        BehaviorGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_wrench = "objects/GravityHelper/gravityController/wrench"

Ahorn.editingIgnored(entity::BehaviorGravityController, multiple::Bool=false) = String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::BehaviorGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BehaviorGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_wrench, 0, 0)
end

end