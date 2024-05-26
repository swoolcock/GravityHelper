-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local fakeTilesHelper = require("helpers.fake_tiles")

local placementData = helpers.createPlacementData('1', {
    width = 16,
    height = 16,
    defaultToController = true,
    leftGravityType = 2,
    rightGravityType = 2,
    topEnabled = true,
    bottomEnabled = true,
    leftEnabled = false,
    rightEnabled = false,
    fallType = 0,
    climbFall = true,
    endFallOnSolidTiles = true,
    sound = "event:/char/badeline/disappear",
    autotile = false,
    tiletype = fakeTilesHelper.getPlacementMaterial(),
    refillDashCount = 0,
    refillStamina = false,
    refillRespawnTime = 2.5,
    giveGravityRefill = false,
    refillOneUse = false,
    blockOneUse = false,
    showEdgeIndicators = true,
})

local inversionBlock = {
    name = "GravityHelper/InversionBlock",
    minimumSize = {16, 16},
    ignoredFields = consts.ignoredFields,
    fieldInformation = function() return {
        leftGravityType = consts.fieldInformation.gravityType(0,1,2),
        rightGravityType = consts.fieldInformation.gravityType(0,1,2),
        fallType = consts.fieldInformation.fallType,
        tiletype = {
            options = fakeTilesHelper.getTilesOptions(),
            editable = false,
        },
        refillDashCount = {
            fieldType = "integer",
        },
        refillRespawnTime = {
            fieldType = "number",
        }
    } end,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
            }),
        },
        {
            name = "sides",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
            }),
        },
    },
}

local fakeTilesSpriteFunction = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local function getEdgeSprite(entity, drawX, drawY, row, column, rotation)
    local edgeTexture = "objects/GravityHelper/inversionBlock/edges"
    local sprite = drawableSprite.fromTexture(edgeTexture, entity)
    sprite:addPosition(drawX, drawY)
    if rotation ~= 0 then
        sprite:addPosition(4, 4)
        sprite:setOffset(4, 4)
        sprite.rotation = rotation
    end
    sprite:useRelativeQuad(column * 8, row * 8, 8, 8)
    return sprite
end

function inversionBlock.sprite(room, entity, node)
    local blockTexture = "objects/GravityHelper/inversionBlock/block"
    local topGravityType, bottomGravityType = 0, 1
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local widthInTiles, heightInTiles = width / 8, height / 8

    local sprites
    if entity.autotile then
        sprites = fakeTilesSpriteFunction(room, entity, node)
    else
        local ninePatch = drawableNinePatch.fromTexture(blockTexture, ninePatchOptions, x, y, width, height)
        sprites = ninePatch:getDrawableSprite()
    end

    if entity.showEdgeIndicators and entity.leftEnabled then
        table.insert(sprites, getEdgeSprite(entity, 0, 0, entity.leftGravityType, 2, -math.pi / 2))
        table.insert(sprites, getEdgeSprite(entity, 0, height - 8, entity.leftGravityType, 0, -math.pi / 2))
        for i = 1,heightInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, 0, i * 8, entity.leftGravityType, 1, -math.pi / 2))
        end
    end

    if entity.showEdgeIndicators and entity.rightEnabled then
        table.insert(sprites, getEdgeSprite(entity, width - 8, 0, entity.rightGravityType, 0, math.pi / 2))
        table.insert(sprites, getEdgeSprite(entity, width - 8, height - 8, entity.rightGravityType, 2, math.pi / 2))
        for i = 1,heightInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, width - 8, i * 8, entity.rightGravityType, 1, math.pi / 2))
        end
    end

    if entity.showEdgeIndicators and entity.topEnabled then
        table.insert(sprites, getEdgeSprite(entity, 0, 0, topGravityType, 0, 0))
        table.insert(sprites, getEdgeSprite(entity, width - 8, 0, topGravityType, 2, 0))
        for i = 1,widthInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, i * 8, 0, topGravityType, 1, 0))
        end
    end

    if entity.showEdgeIndicators and entity.bottomEnabled then
        table.insert(sprites, getEdgeSprite(entity, 0, height - 8, bottomGravityType, 2, math.pi))
        table.insert(sprites, getEdgeSprite(entity, width - 8, height - 8, bottomGravityType, 0, math.pi))
        for i = 1,widthInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, i * 8, height - 8, bottomGravityType, 1, math.pi))
        end
    end

    local refillSprite
    if entity.giveGravityRefill then
        refillSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityRefill/idle00", entity)
    elseif entity.refillDashCount == 2 then
        refillSprite = drawableSprite.fromTexture("objects/refillTwo/idle00", entity)
    elseif entity.refillDashCount > 0 then
        refillSprite = drawableSprite.fromTexture("objects/refill/idle00", entity)
    end

    if refillSprite ~= nil then
        refillSprite:addPosition(entity.width / 2, entity.height / 2)
        table.insert(sprites, refillSprite)
    end

    return sprites
end

return inversionBlock
