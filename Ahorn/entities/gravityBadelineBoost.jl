module GravityHelperGravityBadelineBoost

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityBadelineBoost" GravityBadelineBoost(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    gravityType::Integer=0,
    lockCamera::Bool=true,
    canSkip::Bool=false,
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

Ahorn.editingIgnored(entity::GravityBadelineBoost, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityBadelineBoost) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

Ahorn.nodeLimits(entity::GravityBadelineBoost) = 0, -1

sprite = "objects/badelineboost/idle00.png"

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

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px, py, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)

        px, py = nx, ny
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityBadelineBoost, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, x, y)
end

end