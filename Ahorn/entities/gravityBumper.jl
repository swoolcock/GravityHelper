module GravityHelperGravityBumper

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/GravityBumper" GravityBumper(x::Integer, y::Integer, gravityType::Integer=0)

const placements = Ahorn.PlacementDict(
    "Gravity Bumper (GravityHelper)" => Ahorn.EntityPlacement(
        GravityBumper
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

Ahorn.nodeLimits(entity::GravityBumper) = 0, 1

Ahorn.editingOptions(entity::GravityBumper) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

sprite = "objects/Bumper/Idle22.png"

function Ahorn.selection(entity::GravityBumper)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])
        return [Ahorn.getSpriteRectangle(sprite, x, y), Ahorn.getSpriteRectangle(sprite, nx, ny)]
    end

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityBumper)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])

        theta = atan(y - ny, x - nx)
        Ahorn.drawArrow(ctx, x, y, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)

        gravityType = Int(get(entity.data, "gravityType", 0))
        color = gravityColors[gravityType]
        Ahorn.drawSprite(ctx, sprite, nx, ny, tint=color)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityBumper, room::Maple.Room)
    gravityType = Int(get(entity.data, "gravityType", 0))
    color = gravityColors[gravityType]
    Ahorn.drawSprite(ctx, sprite, 0, 0, tint=color)
end

end