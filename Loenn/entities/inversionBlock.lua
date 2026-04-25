-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local fakeTilesHelper = require("helpers.fake_tiles")

local placementData = helpers.createPlacementData('2', {
    width = 16,
    height = 16,
    defaultToController = true,
    leftGravityType = 2,
    rightGravityType = 2,
    topEnabled = true,
    bottomEnabled = true,
    leftEnabled = false,
    rightEnabled = false,
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
    legacyFallBehavior = false,
    textureDirectory = "",
    gravityRefillTextureDirectory = "",
})

helpers.addFallingData(placementData)
helpers.addSwapData(placementData)
helpers.addZipData(placementData)

local inversionBlock = {
    name = "GravityHelper/InversionBlock",
    minimumSize = {16, 16},
    ignoredFields = function(entity)
        local tbl = helpers.union({}, consts.ignoredFields)
        helpers.addIgnoreComponents(entity, tbl)
        return tbl
    end,
    fieldOrder = {
        "x", "y",
        "width", "height",
        "topEnabled", "bottomEnabled", "leftEnabled", "rightEnabled",
        "leftGravityType", "rightGravityType",
        "refillDashCount", "refillRespawnTime",
        "refillStamina", "refillOneUse", "giveGravityRefill", "blockOneUse",
        "fallType", "climbFall", "endFallOnSolidTiles",
        "tiletype", "sound",
        "autotile", "showEdgeIndicators", "defaultToController", "legacyFallBehavior",
        "textureDirectory", "gravityRefillTextureDirectory",
        "swapType"
    },
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
        },
        fallType = consts.fieldInformation.fallType,
        swapType = consts.fieldInformation.swapType,
        zipType = consts.fieldInformation.zipType,
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
        {
            name = "normal_fall_down",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
                fallType = 1,
            }),
        },
        {
            name = "normal_fall_up",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
                fallType = 2,
            }),
        },
        {
            name = "sides_fall_down",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
                fallType = 1,
            }),
        },
        {
            name = "sides_fall_up",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
                fallType = 2,
            }),
        },
        {
            name = "normal_swap",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
                swapType = 1,
            }),
        },
        {
            name = "sides_swap",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
                swapType = 1,
            }),
        },
        {
            name = "normal_zip",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
                zipType = 1,
            }),
        },
        {
            name = "sides_zip",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
                zipType = 1,
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

local function getEdgeSprite(entity, node, drawX, drawY, row, column, rotation)
    local basePath = "objects/GravityHelper/inversionBlock/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    basePath = helpers.ensureSingleTrailingSlash(basePath)
    local edgeTexture = basePath.."edges"
    local sprite = drawableSprite.fromTexture(edgeTexture, entity)
    if node then
        drawX += node.x - entity.x
        drawY += node.y - entity.y
    end
    sprite:addPosition(drawX, drawY)
    if rotation ~= 0 then
        sprite:addPosition(4, 4)
        sprite:setOffset(4, 4)
        sprite.rotation = rotation
    end
    sprite:useRelativeQuad(column * 8, row * 8, 8, 8)
    return sprite
end

local function mainSprites(room, entity, node)
    local basePath = "objects/GravityHelper/inversionBlock/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    basePath = helpers.ensureSingleTrailingSlash(basePath)
    local blockTexture = basePath.."block"
    local topGravityType, bottomGravityType = 0, 1
    local x, y = node and node.x or entity.x or 0, node and node.y or entity.y or 0
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
        table.insert(sprites, getEdgeSprite(entity, node, 0, 0, entity.leftGravityType, 2, -math.pi / 2))
        table.insert(sprites, getEdgeSprite(entity, node, 0, height - 8, entity.leftGravityType, 0, -math.pi / 2))
        for i = 1,heightInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, node, 0, i * 8, entity.leftGravityType, 1, -math.pi / 2))
        end
    end

    if entity.showEdgeIndicators and entity.rightEnabled then
        table.insert(sprites, getEdgeSprite(entity, node, width - 8, 0, entity.rightGravityType, 0, math.pi / 2))
        table.insert(sprites, getEdgeSprite(entity, node, width - 8, height - 8, entity.rightGravityType, 2, math.pi / 2))
        for i = 1,heightInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, node, width - 8, i * 8, entity.rightGravityType, 1, math.pi / 2))
        end
    end

    if entity.showEdgeIndicators and entity.topEnabled then
        table.insert(sprites, getEdgeSprite(entity, node, 0, 0, topGravityType, 0, 0))
        table.insert(sprites, getEdgeSprite(entity, node, width - 8, 0, topGravityType, 2, 0))
        for i = 1,widthInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, node, i * 8, 0, topGravityType, 1, 0))
        end
    end

    if entity.showEdgeIndicators and entity.bottomEnabled then
        table.insert(sprites, getEdgeSprite(entity, node, 0, height - 8, bottomGravityType, 2, math.pi))
        table.insert(sprites, getEdgeSprite(entity, node, width - 8, height - 8, bottomGravityType, 0, math.pi))
        for i = 1,widthInTiles-2 do
            table.insert(sprites, getEdgeSprite(entity, node, i * 8, height - 8, bottomGravityType, 1, math.pi))
        end
    end

    local refillSprite
    if entity.giveGravityRefill then
        local refillPath = "objects/GravityHelper/gravityRefill/"
        if entity.gravityRefillTextureDirectory and entity.gravityRefillTextureDirectory ~= "" then
            refillPath = entity.gravityRefillTextureDirectory
        end
        refillPath = helpers.ensureSingleTrailingSlash(refillPath)
        refillSprite = helpers.fromTexture(refillPath.."idle00", entity)
    elseif entity.refillDashCount == 2 then
        refillSprite = helpers.fromTexture("objects/refillTwo/idle00", entity)
    elseif entity.refillDashCount > 0 then
        refillSprite = helpers.fromTexture("objects/refill/idle00", entity)
    end

    if refillSprite ~= nil then
        local dx,dy = node and (node.x - entity.x) or 0, node and (node.y - entity.y) or 0
        refillSprite:addPosition(dx + entity.width / 2, dy + entity.height / 2)
        table.insert(sprites, refillSprite)
    end

    return sprites
end

function inversionBlock.sprite(room, entity, node)
    local sprites = mainSprites(room, entity, nil)
    if helpers.isSwapEnabled(entity) then
        helpers.addSwapTrailSprites(sprites, entity)
    elseif helpers.isZipEnabled(entity) then
        helpers.addZipSprites(sprites, entity)
    end
    return sprites
end

function inversionBlock.nodeSprite(room, entity, node, nodeIndex)
    if helpers.isZipEnabled(entity) then return nil end
    return mainSprites(room, entity, node)
end

function inversionBlock.nodeLimits(room, entity)
    if helpers.isSwapEnabled(entity) or helpers.isZipEnabled(entity) then
        return {1, 1}
    else
        return {0, 0}
    end
end

function inversionBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local nodeRects = {}

    if helpers.isSwapEnabled(entity) then
        helpers.addSwapNodeSelection(nodeRects, entity)
    elseif helpers.isZipEnabled(entity) then
        helpers.addZipNodeSelection(nodeRects, entity)
    end

    return utils.rectangle(x, y, width, height), nodeRects
end

return inversionBlock
