# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityRefill

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityRefill" GravityRefill(
    x::Integer, y::Integer,
    pluginVersion::String=PLUGIN_VERSION,
    charges::Integer=1,
    dashes::Integer=-1,
    oneUse::Bool=false,
    refillsDash::Bool=true,
    refillsStamina::Bool=true,
    respawnTime::Float64=2.5
)

const placements = Ahorn.PlacementDict(
    "Gravity Refill (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill
    ),
    "Gravity Refill (Single Use) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill,
        "point",
        Dict{String, Any}(
            "oneUse" => true
        )
    ),
    "Gravity Refill (No Dash/Stamina) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill,
        "point",
        Dict{String, Any}(
            "refillsDash" => false,
            "refillsStamina" => false
        )
    ),
    "Gravity Refill (No Dash/Stamina, Single Use) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill,
        "point",
        Dict{String, Any}(
            "refillsDash" => false,
            "refillsStamina" => false,
            "oneUse" => true
        )
    ),
    "Gravity Refill (Two Dashes/Charges) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 2,
            "charges" => 2
        )
    ),
    "Gravity Refill (Two Dashes/Charges, Single Use) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 2,
            "charges" => 2,
            "oneUse" => true
        )
    ),
)

const normalSprite = "objects/GravityHelper/gravityRefill/idle00"
const noDashSprite = "objects/GravityHelper/gravityRefill/idle_no_dash00"

Ahorn.editingIgnored(entity::GravityRefill, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravityRefill)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 4, y - 5, 8, 10)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityRefill, room::Maple.Room)
    local sprite = get(entity.data, "refillsDash", true) ? normalSprite : noDashSprite
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end