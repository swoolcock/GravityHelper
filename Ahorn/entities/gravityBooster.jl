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
)

const placements = Ahorn.PlacementDict(
    "Gravity Booster (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBooster,
        "point",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Booster (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBooster,
        "point",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Booster (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBooster,
        "point",
        Dict{String, Any}(
            "gravityType" => 2,
        )
    )
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    0 => (0.0, 0.0, 1.0, 1.0),
    1 => (1.0, 0.0, 0.0, 1.0),
    2 => (0.75, 0.0, 0.75, 1.0),
)

Ahorn.editingIgnored(entity::GravityBooster, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityBooster) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

overlay = "objects/GravityHelper/gravityBooster/overlay"
normalSprite = "objects/booster/booster00"
redSprite = "objects/booster/boosterRed00"
ripple = "objects/GravityHelper/ripple03"

function Ahorn.selection(entity::GravityBooster)
    x, y = Ahorn.position(entity)
    sprite = get(entity.data, "red", false) ? redSprite : normalSprite
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityBooster, room::Maple.Room)
    gravityType = get(entity.data, "gravityType", 0)
    sprite = get(entity.data, "red", false) ? redSprite : normalSprite
    color = gravityColors[gravityType]

    Ahorn.drawSprite(ctx, sprite, 0, 0)
    Ahorn.drawSprite(ctx, overlay, 0, 0, tint=color)

    if gravityType == 1 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, 0, -4, tint=color)
    end
    if gravityType == 0 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, 0, 4, sy=-1, tint=color)
    end
end

end