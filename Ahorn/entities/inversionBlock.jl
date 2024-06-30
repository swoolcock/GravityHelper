# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperInversionBlock

using ..Ahorn, Maple

const PLUGIN_VERSION = "2"

const default_sound = "event:/char/badeline/disappear"

@mapdef Entity "GravityHelper/InversionBlock" InversionBlock(
    x::Integer, y::Integer,
    width::Integer=32, height::Integer=32,
    defaultToController::Bool=true,
    leftGravityType::Integer=2, rightGravityType::Integer=2,
    topEnabled::Bool=true, bottomEnabled::Bool=true, leftEnabled::Bool=false, rightEnabled::Bool=false,
    fallType::Integer=0, climbFall::Bool=true, endFallOnSolidTiles::Bool=true,
    sound::String=default_sound,
    autotile::Bool=false, tiletype::String="3",
    refillDashCount::Integer=0, refillStamina::Bool=false, refillRespawnTime::Real=2.5,
    giveGravityRefill::Bool=false, refillOneUse::Bool=false, blockOneUse::Bool=false,
    legacyFallBehavior::Bool=false,
    pluginVersion::String=PLUGIN_VERSION,
)

const placements = Ahorn.PlacementDict(
    "Inversion Block (GravityHelper)" => Ahorn.EntityPlacement(
        InversionBlock,
        "rectangle",
        Dict{String, Any}(
            "topEnabled" => true,
            "bottomEnabled" => true,
            "leftEnabled" => false,
            "rightEnabled" => false,
        ),
    ),
    "Inversion Block (Toggle Sides) (GravityHelper)" => Ahorn.EntityPlacement(
        InversionBlock,
        "rectangle",
        Dict{String, Any}(
            "topEnabled" => false,
            "bottomEnabled" => false,
            "leftEnabled" => true,
            "rightEnabled" => true,
        ),
    ),
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const fallTypes = Dict{String, Integer}(
    "None" => 0,
    "Down" => 1,
    "Up" => 2,
    "Match Player" => 3,
    "Opposite Player" => 4,
)

const blockSprite = "objects/GravityHelper/inversionBlock/block"
const edgeSprite = "objects/GravityHelper/inversionBlock/edges"

Ahorn.editingIgnored(entity::InversionBlock, multiple::Bool=false) = multiple ? String["x", "y", "width", "height", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::InversionBlock) = Dict{String, Any}(
    "leftGravityType" => gravityTypes,
    "rightGravityType" => gravityTypes,
    "fallType" => fallTypes,
    "tiletype" => Ahorn.tiletypeEditingOptions(),
)

Ahorn.minimumSize(entity::InversionBlock) = 16, 16
Ahorn.resizable(entity::InversionBlock) = true, true
Ahorn.selection(entity::InversionBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::InversionBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    leftGravityType = get(entity.data, "leftGravityType", 2)
    rightGravityType = get(entity.data, "rightGravityType", 2)
    topGravityType = 0
    bottomGravityType = 1
    
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    Ahorn.Cairo.save(ctx)

    Ahorn.rectangle(ctx, 0, 0, width, height)
    Ahorn.clip(ctx)
    
    for yy in 0:tilesHeight-1
        for xx in 0:tilesWidth-1
            srcX = xx == 0 ? 0 : xx == tilesWidth-1 ? 2 : 1
            srcY = yy == 0 ? 0 : yy == tilesHeight-1 ? 2 : 1
            Ahorn.drawImage(ctx, blockSprite, xx * 8, yy * 8, srcX * 8, srcY * 8, 8, 8)
        end
    end
    
    if get(entity.data, "leftEnabled", false)
        Ahorn.Cairo.save(ctx)
        Ahorn.translate(ctx, 0, 8)
        Ahorn.rotate(ctx, -pi/2)
        Ahorn.drawImage(ctx, edgeSprite, 0, 0, 16, leftGravityType * 8, 8, 8)
        Ahorn.drawImage(ctx, edgeSprite, -(height-8), 0, 0, leftGravityType * 8, 8, 8)
        for yy in 1:tilesHeight-2
            Ahorn.drawImage(ctx, edgeSprite, -yy * 8, 0, 8, leftGravityType * 8, 8, 8)
        end
        Ahorn.restore(ctx)
    end
    
    if get(entity.data, "rightEnabled", false)
        Ahorn.Cairo.save(ctx)
        Ahorn.translate(ctx, width, height-8)
        Ahorn.rotate(ctx, pi/2)
        Ahorn.drawImage(ctx, edgeSprite, 0, 0, 16, rightGravityType * 8, 8, 8)
        Ahorn.drawImage(ctx, edgeSprite, -(height-8), 0, 0, rightGravityType * 8, 8, 8)
        for yy in 1:tilesHeight-2
            Ahorn.drawImage(ctx, edgeSprite, -yy * 8, 0, 8, rightGravityType * 8, 8, 8)
        end
        Ahorn.restore(ctx)
    end
    
    if get(entity.data, "topEnabled", false)
        Ahorn.drawImage(ctx, edgeSprite, 0, 0, 0, topGravityType * 8, 8, 8)
        Ahorn.drawImage(ctx, edgeSprite, width-8, 0, 16, topGravityType * 8, 8, 8)
        for xx in 1:tilesWidth-2
            Ahorn.drawImage(ctx, edgeSprite, xx * 8, 0, 8, topGravityType * 8, 8, 8)
        end
    end
    
    if get(entity.data, "bottomEnabled", false)
        Ahorn.Cairo.save(ctx)
        Ahorn.translate(ctx, width, height-8)
        Ahorn.rotate(ctx, pi)
        Ahorn.drawImage(ctx, edgeSprite, width-8, -8, 16, bottomGravityType * 8, 8, 8)
        Ahorn.drawImage(ctx, edgeSprite, 0, -8, 0, bottomGravityType * 8, 8, 8)
        for xx in 1:tilesWidth-2
            Ahorn.drawImage(ctx, edgeSprite, xx * 8, -8, 8, bottomGravityType * 8, 8, 8)
        end
        Ahorn.restore(ctx)
    end

    if get(entity.data, "giveGravityRefill", false)
        Ahorn.drawSprite(ctx, "objects/GravityHelper/gravityRefill/idle00", width / 2, height / 2)
    elseif get(entity.data, "refillDashCount", 0) == 2
        Ahorn.drawSprite(ctx, "objects/refillTwo/idle00", width / 2, height / 2)
    elseif get(entity.data, "refillDashCount", 0) > 0
        Ahorn.drawSprite(ctx, "objects/refill/idle00", width / 2, height / 2)
    end

    Ahorn.restore(ctx)
end

end