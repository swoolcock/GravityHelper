# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperSoundGravityController

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

const default_normal_gravity_sound = "event:/ui/game/lookout_off"
const default_inverted_gravity_sound = "event:/ui/game/lookout_on"
const default_line_sound = "event:/gravityhelper/gravity_line"
const default_inversion_block_sound = "event:/char/badeline/disappear"

@mapdef Entity "GravityHelper/SoundGravityController" SoundGravityController(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    persistent::Bool=true,
    normalSound::String=default_normal_gravity_sound,
    invertedSound::String=default_inverted_gravity_sound,
    toggleSound::String="",
    lineSound::String=default_line_sound,
    inversionBlockSound::String=default_inversion_block_sound,
    musicParam::String=""
)

const placements = Ahorn.PlacementDict(
    "Sound Gravity Controller (Single Room) (GravityHelper)" => Ahorn.EntityPlacement(
        SoundGravityController,
        "point",
        Dict{String, Any}(
            "persistent" => false,
        )
    ),
    "Sound Gravity Controller (Persistent) (GravityHelper)" => Ahorn.EntityPlacement(
        SoundGravityController,
    ),
)

const sprite = "objects/GravityHelper/gravityController/circle"
const sprite_dot = "objects/GravityHelper/gravityController/circle_dot"
const sprite_speaker = "objects/GravityHelper/gravityController/speaker"

Ahorn.editingIgnored(entity::SoundGravityController, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::SoundGravityController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SoundGravityController)
    icon = get(entity.data, "persistent", false) ? sprite_dot : sprite
    Ahorn.drawSprite(ctx, icon, 0, 0)
    Ahorn.drawSprite(ctx, sprite_speaker, 0, 0)
end

end