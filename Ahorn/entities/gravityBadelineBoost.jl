# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityBadelineBoost

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityBadelineBoost" GravityBadelineBoost(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    gravityType::Integer=0,
    lockCamera::Bool=true,
    canSkip::Bool=false,
    nodeGravityTypes::String="",
)

const placements = Ahorn.PlacementDict(
    "Gravity Badeline Boost (Gravity Helper)" => Ahorn.EntityPlacement(
        GravityBadelineBoost
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
    "None" => -1,
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    0 => (0.0, 0.0, 1.0, 1.0),
    1 => (1.0, 0.0, 0.0, 1.0),
    2 => (0.75, 0.0, 0.75, 1.0),
    -1 => (1.0, 1.0, 1.0, 1.0),
)

Ahorn.editingIgnored(entity::GravityBadelineBoost, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityBadelineBoost) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

Ahorn.nodeLimits(entity::GravityBadelineBoost) = 0, -1

sprite = "objects/badelineboost/idle00.png"
mask = "objects/GravityHelper/gravityBadelineBoost/mask00"
ripple = "objects/GravityHelper/ripple03"

function drawNode(ctx::Ahorn.Cairo.CairoContext, x::Real, y::Real, gravityType::Integer)
    gravityType = clamp(gravityType, -1, 2)
    color = gravityColors[gravityType]
    Ahorn.drawSprite(ctx, mask, x, y, tint=color)
    Ahorn.drawSprite(ctx, sprite, x, y)

    if gravityType == 0 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, x, y - 4, tint=color)
    end
    if gravityType == 1 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, x, y + 4, sy=-1, tint=color)
    end
end

function Ahorn.selection(entity::GravityBadelineBoost)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

    for node in nodes
        nx, ny = Int.(node)
        push!(res, Ahorn.getSpriteRectangle(sprite, nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityBadelineBoost)
    px, py = Ahorn.position(entity)
    globalType = get(entity.data, "gravityType", 0)
    nodeTypesString = get(entity.data, "nodeGravityTypes", "")
    nodeTypes = tryparse.(Int, split(nodeTypesString, ','))
    nodes = get(entity.data, "nodes", ())

    for (i, node) in enumerate(nodes)
        nx, ny = Int.(node)
        theta = atan(py - ny, px - nx)
        nodeType = get(nodeTypes, i + 1, globalType)
        Ahorn.drawArrow(ctx, px, py, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        drawNode(ctx, nx, ny, nodeType)
        px, py = nx, ny
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityBadelineBoost, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodeTypesString = get(entity.data, "nodeGravityTypes", "")
    nodeTypes = tryparse.(Int, split(nodeTypesString, ','))
    globalType = get(entity.data, "gravityType", 0)
    gravityType = get(nodeTypes, 1, globalType)
    drawNode(ctx, x, y, gravityType)
end

end