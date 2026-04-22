-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local utils = require("utils")

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

function helpers.parseHexColor(str, alpha)
    local parsed, r, g, b = utils.parseHexColor(str)
    return parsed and { r, g, b, alpha or 1 } or { 0, 0, 0, 0 }
end

function helpers.createPlacementData(pluginVersion, data)
    return helpers.union({
        modVersion = consts.modVersion,
        pluginVersion = pluginVersion,
    }, data)
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

function helpers.addIgnoreComponents(entity, tbl)
    if entity and entity.swapType == 0 then
        helpers.addIgnoreSwap(tbl)
    else
        helpers.addIgnoreFalling(tbl)
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

function helpers.tryAddSwapTrailSprites(sprites, entity)
    if helpers.isSwapEnabled(entity) then
        helpers.addSwapTrailSprites(sprites, entity)
    end
end

function helpers.isSwapEnabled(entity)
    return entity.swapType ~= nil and entity.swapType > 0
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

return helpers
