# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityBumper

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityBumper" GravityBumper(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    gravityType::Integer=0,
    wobbleRate::Real=1.0
)

const placements = Ahorn.PlacementDict(
    "Gravity Bumper (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBumper,
        "point",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Bumper (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBumper,
        "point",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Bumper (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBumper,
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

Ahorn.nodeLimits(entity::GravityBumper) = 0, 1

Ahorn.editingIgnored(entity::GravityBumper, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityBumper) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

sprite = "objects/Bumper/Idle22.png"
mask = "objects/GravityHelper/gravityBumper/mask00"
ripple = "objects/GravityHelper/ripple03"

function Ahorn.selection(entity::GravityBumper)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])
        return [Ahorn.getSpriteRectangle(sprite, x, y), Ahorn.getSpriteRectangle(sprite, nx, ny)]
    end

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function drawNode(ctx::Ahorn.Cairo.CairoContext, x::Real, y::Real, gravityType::Integer)
    gravityType = clamp(gravityType, -1, 2)
    color = gravityColors[gravityType]
    Ahorn.drawSprite(ctx, mask, x, y, tint=color)
    Ahorn.drawSprite(ctx, sprite, x, y)

    if gravityType == 1 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, x, y - 8, tint=color)
    end
    if gravityType == 0 || gravityType == 2
        Ahorn.drawSprite(ctx, ripple, x, y + 8, sy=-1, tint=color)
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityBumper)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    gravityType = get(entity.data, "gravityType", 0)

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])

        theta = atan(y - ny, x - nx)
        Ahorn.drawArrow(ctx, x, y, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        drawNode(ctx, nx, ny, gravityType)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityBumper, room::Maple.Room)
    gravityType = Int(get(entity.data, "gravityType", 0))
    drawNode(ctx, 0, 0, gravityType)
end

end