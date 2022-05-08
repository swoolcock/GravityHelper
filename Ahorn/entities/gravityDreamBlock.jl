# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityDreamBlock

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityDreamBlock" GravityDreamBlock(
    x::Integer, y::Integer,
    width::Integer=8, height::Integer=8,
    pluginVersion::String=PLUGIN_VERSION,
    fastMoving::Bool=false, oneUse::Bool=false, below::Bool=false,
    gravityType::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Gravity Dream Block (GravityHelper)" => Ahorn.EntityPlacement(
        GravityDreamBlock,
        "rectangle"
    )
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

Ahorn.editingIgnored(entity::GravityDreamBlock, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityDreamBlock) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

Ahorn.nodeLimits(entity::GravityDreamBlock) = 0, 1
Ahorn.minimumSize(entity::GravityDreamBlock) = 8, 8
Ahorn.resizable(entity::GravityDreamBlock) = true, true

function Ahorn.selection(entity::GravityDreamBlock)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    nodes = get(entity.data, "nodes", ())

    if isempty(nodes)
        return Ahorn.Rectangle(x, y, width, height)
    else
        nx, ny = Int.(nodes[1])
        return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx, ny, width, height)]
    end
end

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, entity::GravityDreamBlock, x::Number, y::Number, width::Number, height::Number)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    gravityType = Int(get(entity.data, "gravityType", 0))
    color = gravityColors[gravityType]

    Ahorn.drawRectangle(ctx, x, y, width, height, color, (1.0, 1.0, 1.0, 1.0))

    Ahorn.restore(ctx)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityDreamBlock)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])
        cox, coy = floor(Int, width / 2), floor(Int, height / 2)
        renderSpaceJam(ctx, entity, nx, ny, width, height)
        Ahorn.drawArrow(ctx, x + cox, y + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength=6)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityDreamBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderSpaceJam(ctx, entity, 0, 0, width, height)
end

end