module GravityHelperVvvvvvGravityController

using ..Ahorn, Maple

const default_vvvvvv_sound = "event:/gravityhelper/toggle"

@mapdef Entity "GravityHelper/VvvvvvGravityController" VvvvvvGravityController(
    x::Integer, y::Integer,
    persistent::Bool=true,
    mode::Integer=2,
    disableGrab::Bool=true,
    disableDash::Bool=true,
    flipSound::String=default_vvvvvv_sound
)

const placements = Ahorn.PlacementDict(
    "VVVVVV Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        VvvvvvGravityController,
    ),
)

const vvvvvvModes = Dict{String, Integer}(
    "Trigger-based" => 0,
    "Off" => 1,
    "On" => 2,
)

Ahorn.editingOptions(entity::VvvvvvGravityController) = Dict{String, Any}(
    "mode" => vvvvvvModes
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_spikes = "objects/GravityHelper/gravityController/spikes"

function Ahorn.selection(entity::VvvvvvGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::VvvvvvGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_spikes, 0, 0)
end

end