module GravityHelperGravitySpring

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

const default_cooldown = 0.1

@mapdef Entity "GravityHelper/GravitySpringFloor" GravitySpringFloor(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    playerCanUse::Bool=true, gravityType::Integer=0, gravityCooldown::Real=default_cooldown)
@mapdef Entity "GravityHelper/GravitySpringWallLeft" GravitySpringWallLeft(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    playerCanUse::Bool=true, gravityType::Integer=2, gravityCooldown::Real=default_cooldown)
@mapdef Entity "GravityHelper/GravitySpringWallRight" GravitySpringWallRight(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    playerCanUse::Bool=true, gravityType::Integer=2, gravityCooldown::Real=default_cooldown)
@mapdef Entity "GravityHelper/GravitySpringCeiling" GravitySpringCeiling(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    playerCanUse::Bool=true, gravityType::Integer=1, gravityCooldown::Real=default_cooldown)

const gravitySpringUnion = Union{GravitySpringFloor, GravitySpringWallLeft, GravitySpringWallRight, GravitySpringCeiling}

const gravityTypes = Dict{String, Integer}(
    "None" => -1,
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const sprites = Dict{Integer, String}(
    -1 => "objects/GravityHelper/gravitySpring/none00.png",
    0 => "objects/GravityHelper/gravitySpring/normal00.png",
    1 => "objects/GravityHelper/gravitySpring/invert00.png",
    2 => "objects/GravityHelper/gravitySpring/toggle00.png",
)

const placements = Ahorn.PlacementDict(
    "Gravity Spring (Up) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySpringFloor,
    ),
    "Gravity Spring (Down) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySpringCeiling,
    ),
    "Gravity Spring (Right) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySpringWallLeft,
    ),
    "Gravity Spring (Left) (GravityHelper)" => Ahorn.EntityPlacement(
        GravitySpringWallRight,
    ),
)

Ahorn.editingIgnored(entity::gravitySpringUnion, multiple::Bool=false) = String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravitySpringFloor)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 6, y - 3, 12, 5)
end

function Ahorn.selection(entity::GravitySpringCeiling)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 6, y - 2, 12, 5)
end

function Ahorn.selection(entity::GravitySpringWallLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 1, y - 6, 5, 12)
end

function Ahorn.selection(entity::GravitySpringWallRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 4, y - 6, 5, 12)
end

Ahorn.editingOptions(entity::gravitySpringUnion) = Dict{String, Any}( "gravityType" => gravityTypes )

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravitySpringFloor, room::Maple.Room) = Ahorn.drawSprite(ctx, sprites[get(entity.data, "gravityType", -1)], 0, 0, rot=0, jx=0.5, jy=1.0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravitySpringCeiling, room::Maple.Room) = Ahorn.drawSprite(ctx, sprites[get(entity.data, "gravityType", -1)], 0, 0, rot=0, sy=-1.0, jx=0.5, jy=1.0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravitySpringWallLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, sprites[get(entity.data, "gravityType", -1)], 8, 24, rot=-pi/2, sx=-1.0, jx=0.5, jy=1.0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravitySpringWallRight, room::Maple.Room) = Ahorn.drawSprite(ctx, sprites[get(entity.data, "gravityType", -1)], -8, 24, rot=-pi/2, jx=0.5, jy=1.0)

end