# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityBooster

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityBooster" GravityBooster(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    gravityType::Integer=0,
    red::Bool=false,
    useTintedSprites::Bool=true,
)

const placements = Ahorn.PlacementDict(
    "Gravity Booster (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBooster
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    0 => (0.0, 0.0, 1.0, 0.5),
    1 => (1.0, 0.0, 0.0, 0.5),
    2 => (0.5, 0.0, 0.5, 0.5),
)

Ahorn.editingIgnored(entity::GravityBooster, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityBooster) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

normalSprite = "objects/booster/booster00"
redSprite = "objects/booster/boosterRed00"

function Ahorn.selection(entity::GravityBooster)
    x, y = Ahorn.position(entity)
    sprite = get(entity.data, "red", false) ? redSprite : normalSprite
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityBooster, room::Maple.Room)
    gravityType = get(entity.data, "gravityType", 0)
    useTintedSprites = get(entity.data, "useTintedSprites", true)
    sprite = get(entity.data, "red", false) ? redSprite : normalSprite

    if useTintedSprites
        Ahorn.drawSprite(ctx, sprite, 0, 0, tint=gravityColors[gravityType])
    else
        Ahorn.drawSprite(ctx, sprite, 0, 0)
    end
end

end