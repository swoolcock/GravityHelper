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
const twoDashSprite = "objects/GravityHelper/gravityRefill/idle_two_dash00"

function spriteForEntity(entity::GravityRefill)
    local refillsDash = get(entity.data, "refillsDash", true)
    local dashes = get(entity.data, "dashes", -1)
    return refillsDash && dashes == 2 ? twoDashSprite : !refillsDash ? noDashSprite : normalSprite
end

Ahorn.editingIgnored(entity::GravityRefill, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

function Ahorn.selection(entity::GravityRefill)
    x, y = Ahorn.position(entity)
    local sprite = spriteForEntity(entity)
    return sprite == twoDashSprite ? Ahorn.Rectangle(x - 5, y - 7, 10, 14) : Ahorn.Rectangle(x - 4, y - 5, 8, 10)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityRefill, room::Maple.Room)
    local sprite = spriteForEntity(entity)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end