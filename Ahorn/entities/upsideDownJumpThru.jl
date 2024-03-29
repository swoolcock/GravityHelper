# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperUpsideDownJumpThru

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/UpsideDownJumpThru" UpsideDownJumpThru(
    x::Integer, y::Integer,
    width::Integer=8,
    pluginVersion::String=PLUGIN_VERSION,
    texture::String="wood",
    surfaceIndex::Int=-1,
    attached::Bool=false,
    triggerStaticMovers::Bool=true
)

const textures = ["wood", "dream", "temple", "templeB", "cliffside", "reflection", "core", "moon"]
const placements = Ahorn.PlacementDict(
    "Upside Down Jump Through (GravityHelper)" => Ahorn.EntityPlacement(
        UpsideDownJumpThru,
        "rectangle",
        Dict{String, Any}(
            "texture" => "wood"
        )
    ),
    "Upside Down Jump Through (Attached) (GravityHelper)" => Ahorn.EntityPlacement(
        UpsideDownJumpThru,
        "rectangle",
        Dict{String, Any}(
            "texture" => "wood",
            "attached" => true
        )
    )
)

const quads = Tuple{Integer, Integer, Integer, Integer}[
    (0, 0, 8, 7) (8, 0, 8, 7) (16, 0, 8, 7);
    (0, 8, 8, 5) (8, 8, 8, 5) (16, 8, 8, 5)
]

Ahorn.editingIgnored(entity::UpsideDownJumpThru, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::UpsideDownJumpThru) = Dict{String, Any}(
    "texture" => textures,
    "surfaceIndex" => Maple.tileset_sound_ids
)

Ahorn.minimumSize(entity::UpsideDownJumpThru) = 8, 0
Ahorn.resizable(entity::UpsideDownJumpThru) = true, false

function Ahorn.selection(entity::UpsideDownJumpThru)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))

    return Ahorn.Rectangle(x, y, width, 8)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::UpsideDownJumpThru, room::Maple.Room)
    texture = get(entity.data, "texture", "wood")
    texture = texture == "default" ? "wood" : texture

    # Values need to be system specific integer
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 8))

    startX = div(x, 8) + 1
    stopX = startX + div(width, 8) - 1
    startY = div(y, 8) + 1
    animated = Number(get(entity.data, "animationDelay", 0)) > 0

    Ahorn.Cairo.save(ctx)

    Ahorn.scale(ctx, 1, -1)

    len = stopX - startX
    for i in 0:len
        if animated
            Ahorn.drawImage(ctx, "objects/jumpthru/$(texture)00", 8 * i, -8)
        else
            connected = false
            qx = 2
            if i == 0
                connected = get(room.fgTiles.data, (startY, startX - 1), false) != '0'
                qx = 1

            elseif i == len
                connected = get(room.fgTiles.data, (startY, stopX + 1), false) != '0'
                qx = 3
            end

            quad = quads[2 - connected, qx]
            Ahorn.drawImage(ctx, "objects/jumpthru/$(texture)", 8 * i, -8, quad...)
        end
    end

    Ahorn.Cairo.restore(ctx)
end

end