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
    gravityType::Integer=0,
    lineColor::String="", backColor::String="", particleColor::String="",
    fall::Bool=false, climbFall::Bool=true
)

const placements = Ahorn.PlacementDict(
    "Gravity Dream Block (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityDreamBlock,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Dream Block (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityDreamBlock,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Dream Block (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityDreamBlock,
        "rectangle",
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
    0 => (0.0, 0.0, 1.0, 0.5),
    1 => (1.0, 0.0, 0.0, 0.5),
    2 => (0.5, 0.0, 0.5, 0.5),
)

Ahorn.editingIgnored(entity::GravityDreamBlock, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityDreamBlock) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

Ahorn.nodeLimits(entity::GravityDreamBlock) = 0, 1
Ahorn.minimumSize(entity::GravityDreamBlock) = 8, 8
Ahorn.resizable(entity::GravityDreamBlock) = true, true

upArrowSprite = "objects/GravityHelper/gravityDreamBlock/upArrow"
downArrowSprite = "objects/GravityHelper/gravityDreamBlock/downArrow"
doubleArrowSprite = "objects/GravityHelper/gravityDreamBlock/doubleArrow"

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

function lightened(color::Tuple{Real, Real, Real, Real}, amount::Real)
    return (clamp(color[0] + amount, 0, 1), clamp(color[1] + amount, 0, 1), clamp(color[2] + amount, 0, 1), color[3])
end

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, entity::GravityDreamBlock, x::Number, y::Number, width::Number, height::Number)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    gravityType = Int(get(entity.data, "gravityType", 0))
    sprite = gravityType == 0 ? downArrowSprite : gravityType == 1 ? upArrowSprite : doubleArrowSprite
    gravityColor = gravityColors[gravityType]
    entityLineColor = get(entity.data, "lineColor", "")
    entityBackColor = get(entity.data, "backColor", "")
    entityParticleColor = get(entity.data, "particleColor", "")
    lineColor = clamp.(gravityColor .+ 0.4, 0.0, 1.0)
    fillColor = lineColor
    particleColor = lineColor
    
    parsedLineColor = parseColor(entityLineColor)
    parsedBackColor = parseColor(entityBackColor)
    parsedParticleColor = parseColor(entityParticleColor)

    if parsedLineColor != nothing
        lineColor = parsedLineColor
    end

    if parsedBackColor != nothing
        fillColor = parsedBackColor
    end
    
    if parsedParticleColor != nothing
        particleColor = parsedParticleColor
    end
    
    fillColor = (fillColor[1] * 0.12, fillColor[2] * 0.12, fillColor[3] * 0.12, 1.0)
    particleColor = (particleColor[1], particleColor[2], particleColor[3], 1.0)
    
    Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, (lineColor[1], lineColor[2], lineColor[3], 1.0))

    Ahorn.drawSprite(ctx, sprite, x + width / 2, y + height / 2, tint=particleColor)
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

function parseColor(hex::String)
    if length(hex) != 6
        return nothing
    end
    parsed = tryparse(Int, hex, base=16)
    return parsed != nothing ? Ahorn.argb32ToRGBATuple(parsed)[1:3] ./ 255 : nothing
end

end