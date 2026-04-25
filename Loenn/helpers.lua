-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local utils = require("utils")

local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableNinePatch = require("structs.drawable_nine_patch")

local helpers = {}

function helpers.union(...)
    local tbl = {}
    local source = {...}
    for _,t in ipairs(source) do
        for k,v in pairs(t) do tbl[k] = v end
    end
    return tbl
end

function helpers.colorWithAlpha(color, alpha)
    return { color[1], color[2], color[3], alpha }
end

function helpers.parseHexColor(str, alpha, def)
    local parsed, r, g, b = utils.parseHexColor(str)
    return parsed and { r, g, b, alpha or 1 } or def or { 0, 0, 0, 0 }
end

function helpers.createPlacementData(pluginVersion, data)
    return helpers.union({
        modVersion = consts.modVersion,
        pluginVersion = pluginVersion,
    }, data)
end

function helpers.ensureSingleTrailingSlash(path)
    path = path:gsub("/+$", "")
    return path.."/"
end

function helpers.missingImage(entity)
    return drawableSprite.fromTexture("@Internal@/missing_image", entity)
end

function helpers.fromTexture(texture, entity)
    return texture and drawableSprite.fromTexture(texture, entity) or helpers.missingImage(entity)
end

function helpers.addFallingData(tbl, defaults)
    tbl.fallType = 0
    tbl.climbFall = true
    tbl.endFallOnSolidTiles = true
    tbl.invertFallingDirFlag = ""
    if defaults then helpers.union(tbl, defaults) end
end

function helpers.addSwapData(tbl, defaults)
    tbl.swapType = 0
    if defaults then helpers.union(tbl, defaults) end
end

function helpers.addZipData(tbl, defaults)
    tbl.zipType = 0
    if defaults then helpers.union(tbl, defaults) end
end

function helpers.addIgnoreComponents(entity, tbl)
    if helpers.isSwapEnabled(entity) then
        helpers.addIgnoreZip(tbl)
        helpers.addIgnoreFalling(tbl)
    elseif helpers.isZipEnabled(entity) then
        helpers.addIgnoreFalling(tbl)
        helpers.addIgnoreSwap(tbl)
    else
        helpers.addIgnoreZip(tbl)
        helpers.addIgnoreSwap(tbl)
    end
end

function helpers.addIgnoreFalling(tbl)
    table.insert(tbl, "fallType")
    table.insert(tbl, "climbFall")
    table.insert(tbl, "endFallOnSolidTiles")
    table.insert(tbl, "invertFallingDirFlag")
end

function helpers.addIgnoreSwap(tbl)
    table.insert(tbl, "swapType")
end

function helpers.addIgnoreZip(tbl)
    table.insert(tbl, "zipType")
end

function helpers.tryAddSwapTrailSprites(sprites, entity)
    if helpers.isSwapEnabled(entity) then
        helpers.addSwapTrailSprites(sprites, entity)
    end
end

function helpers.isSwapEnabled(entity)
    return entity.swapType ~= nil and entity.swapType > 0
end

function helpers.isZipEnabled(entity)
    return entity.zipType ~= nil and entity.zipType > 0
end

function helpers.isFallingEnabled(entity)
    return
        entity.fallType ~= nil and entity.fallType > 0 or
        entity.fall == true -- will be nil if not legacy entity
end

function helpers.addSwapTrailSprites(sprites, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8
    local drawWidth, drawHeight = math.abs(x - nodeX) + width, math.abs(y - nodeY) + height

    x, y = math.min(x, nodeX), math.min(y, nodeY)

    local trailNinePatchOptions = {
        mode = "fill",
        borderMode = "repeat",
        useRealSize = true
    }

    local trailDepth = 8999

    local trailTexture = "objects/swapblock/target"
    local frameNinePatch = drawableNinePatch.fromTexture(trailTexture, trailNinePatchOptions, x, y, drawWidth, drawHeight)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        sprite.depth = trailDepth
        table.insert(sprites, sprite)
    end
end

function helpers.addZipSprites(sprites, entity)
    local x, y = entity.x or 0, entity.y or 0
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y
    local centerX, centerY = x + halfWidth, y + halfHeight
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local cogTexture = "objects/zipmover/cog"
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture, entity)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local ropeColor = helpers.parseHexColor(entity.zipRopeColor or "663931", nil, {102 / 255, 57 / 255, 49 / 255})
    local leftLine = drawableLine.fromPoints(points, ropeColor, 1)
    local rightLine = drawableLine.fromPoints(points, ropeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = 5000
    rightLine.depth = 5000

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

function helpers.addSwapNodeSelection(rects, entity)
    local nodes = entity.nodes
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes and nodes[1].x or x, nodes and nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8
    table.insert(rects, utils.rectangle(nodeX, nodeY, width, height))
end

function helpers.addZipNodeSelection(rects, entity)
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local cogSprite = drawableSprite.fromTexture("objects/zipmover/cog", entity)
    local cogWidth, cogHeight = cogSprite.meta.width, cogSprite.meta.height

    local nodeRectangle = utils.rectangle(centerNodeX - math.floor(cogWidth / 2), centerNodeY - math.floor(cogHeight / 2), cogWidth, cogHeight)

    table.insert(rects, nodeRectangle)
end

return helpers
