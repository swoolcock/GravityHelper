module GravityHelperVvvvvvGravityController

using ..Ahorn, Maple

const default_vvvvvv_sound = "event:/gravityhelper/toggle"

@mapdef Entity "GravityHelper/VvvvvvGravityController" VvvvvvGravityController(
    x::Integer, y::Integer,
    persistent::Bool=false,
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

const sprite = "objects/GravityHelper/gravityController/icon"

function Ahorn.selection(entity::VvvvvvGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::VvvvvvGravityController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end