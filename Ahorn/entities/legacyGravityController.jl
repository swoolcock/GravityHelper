module GravityHelperGravityController

using ..Ahorn, Maple

const default_normal_gravity_sound = "event:/ui/game/lookout_off"
const default_inverted_gravity_sound = "event:/ui/game/lookout_on"
const default_gravity_music_param = "flip"
const default_vvvvvv_sound = "event:/gravityhelper/toggle"

@mapdef Entity "GravityHelper/GravityController" GravityController(
    x::Integer, y::Integer,
    alwaysTrigger::Bool=false,
    normalGravitySound::String=default_normal_gravity_sound,
    invertedGravitySound::String=default_inverted_gravity_sound,
    toggleGravitySound::String="",
    arrowOpacity::Real=0.5, fieldOpacity::Real=0.15, particleOpacity::Real=0.5,
    holdableResetTime::Real=2.0,
    gravityMusicParam::String=default_gravity_music_param,
    vvvvvv::Bool=false, vvvvvvSound::String=default_vvvvvv_sound
)

const placements = Ahorn.PlacementDict(
    "Legacy Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        GravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/icon"

function Ahorn.selection(entity::GravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end