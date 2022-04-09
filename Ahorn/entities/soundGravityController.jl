module GravityHelperSoundGravityController

using ..Ahorn, Maple

const default_normal_gravity_sound = "event:/ui/game/lookout_off"
const default_inverted_gravity_sound = "event:/ui/game/lookout_on"

@mapdef Entity "GravityHelper/SoundGravityController" SoundGravityController(
    x::Integer, y::Integer,
    persistent::Bool=false,
    normalSound::String=default_normal_gravity_sound,
    invertedSound::String=default_inverted_gravity_sound,
    toggleSound::String="",
    musicParam::String=""
)

const placements = Ahorn.PlacementDict(
    "Sound Gravity Controller (GravityHelper)" => Ahorn.EntityPlacement(
        SoundGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/icon"

function Ahorn.selection(entity::SoundGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SoundGravityController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end