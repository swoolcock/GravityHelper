-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")
--local colors = require("consts.xna_colors")

local drawableSprite = require("structs.drawable_sprite")
local drawableFunction = require("structs.drawable_function")

local doubleArrowTexture = "objects/GravityHelper/gravityDreamBlock/doubleArrow"
local doubleArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/doubleArrowSmall"
local upArrowTexture = "objects/GravityHelper/gravityDreamBlock/upArrow"
local upArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/upArrowSmall"
local downArrowTexture = "objects/GravityHelper/gravityDreamBlock/downArrow"
local downArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/downArrowSmall"

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    fastMoving = false,
    oneUse = false,
    below = false,
    gravityType = consts.gravityTypes.normal.index,
    lineColor = "",
    backColor = "",
    particleColor = "",
    fallType = 0,
    climbFall = true,
    endFallOnSolidTiles = true,
    invertFallingDirFlag = "",
    swapType = 0,
})

local gravityDreamBlock = {
    name = "GravityHelper/GravityDreamBlock",
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
        fallType = consts.fieldInformation.fallType,
        swapType = consts.fieldInformation.swapType,
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
            }),
        },
        {
            name = "inverted",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
            }),
        },
        {
            name = "toggle",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
            }),
        },
        {
            name = "normal_swap",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
                swapType = 1,
            }),
        },
        {
            name = "inverted_swap",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
                swapType = 1,
            }),
        },
        {
            name = "toggle_swap",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
                swapType = 1,
            }),
        },
    },
}

local function lightened(color, lightness, alpha)
    return {
        math.max(0, math.min(1, color[1] + lightness)),
        math.max(0, math.min(1, color[2] + lightness)),
        math.max(0, math.min(1, color[3] + lightness)),
        alpha or color[4]
    }
end

function gravityDreamBlock.depth(room, entity)
    return entity.below and 4999 or -11001
end

local function putInside(entity, x, y, node)
    local ex = node and node.x or entity.x
    local ey = node and node.y or entity.y

    local right, bottom = ex + entity.width, ey + entity.height
    if x > right then
        x = x - math.ceil((x - right) / entity.width) * entity.width
    elseif x < ex then
        x = x + math.ceil((ex - x) / entity.width) * entity.width
    end
    if y > bottom then
        y = y - math.ceil((y - bottom) / entity.height) * entity.height
    elseif y < ey then
        y = y + math.ceil((ey - y) / entity.height) * entity.height
    end
    return x, y
end

local function mainSprite(room, entity, node)
    -- find sprites
    local largeSprite =
    entity.gravityType == 0 and drawableSprite.fromTexture(downArrowTexture) or
            entity.gravityType == 1 and drawableSprite.fromTexture(upArrowTexture) or
            drawableSprite.fromTexture(doubleArrowTexture)
    local smallSprite =
    entity.gravityType == 0 and drawableSprite.fromTexture(downArrowSmallTexture) or
            entity.gravityType == 1 and drawableSprite.fromTexture(upArrowSmallTexture) or
            drawableSprite.fromTexture(doubleArrowSmallTexture)

    -- custom random so we don't poison the global seed
    local seed = 0
    local function rand()
        local a = 1664525
        local c = 1013904223
        local m = 2^32
        seed = (a * seed + c) % m
        return seed / m
    end

    local spr = drawableFunction.fromFunction(function()
        local gravityColor = consts.gravityTypeForIndex(entity.gravityType).color
        local lineColor, backColor, particleColor = lightened(gravityColor, 0.4), lightened(gravityColor, 0.4), gravityColor
        local parsed, r, g, b, a, oldAlpha
        local ex,ey = node and node.x or entity.x, node and node.y or entity.y

        parsed, r, g, b = utils.parseHexColor(entity.lineColor or "")
        if parsed then lineColor = {r, g, b} end

        parsed, r, g, b = utils.parseHexColor(entity.backColor or "")
        if parsed then backColor = {r, g, b} end

        parsed, r, g, b = utils.parseHexColor(entity.particleColor or "")
        if parsed then particleColor = {r, g, b} end

        -- fill
        r, g, b, oldAlpha = love.graphics.getColor()
        a = oldAlpha * (node and 0.75 or 1)

        love.graphics.setColor(0, 0, 0, a)
        love.graphics.rectangle("fill", ex, ey, entity.width, entity.height)
        love.graphics.setColor(backColor[1], backColor[2], backColor[3], a * 0.12)
        love.graphics.rectangle("fill", ex, ey, entity.width, entity.height)

        -- set scissor for particles
        local sx, sy, sw, sh = love.graphics.getScissor()
        local x1, y1 = love.graphics.transformPoint(ex, ey)
        local x2, y2 = love.graphics.transformPoint(ex + entity.width, ey + entity.height)
        love.graphics.setScissor(x1, y1, x2 - x1, y2 - y1)

        -- set seed
        seed = entity._id or 1

        -- render particles
        local particleCount = math.floor((entity.width / 8) * (entity.height / 8) * 0.7)
        for _ = 1,particleCount do
            -- get random values
            local px,py = math.floor(rand() * entity.width), math.floor(rand() * entity.height)
            local lightness = rand() - 0.25
            local layer = math.floor(rand() * 6)
            -- calculate visuals
            local adjusted = lightened(particleColor, lightness, 0.8)
            adjusted[4] *= a

            px, py = putInside(entity, px, py, node)

            if layer < 3 then
                local particleSprite = layer < 1 and largeSprite or smallSprite
                particleSprite.x = px
                particleSprite.y = py
                particleSprite.color = adjusted
                particleSprite:draw()
            else
                love.graphics.setColor(adjusted)
                love.graphics.rectangle("fill", px, py, 1, 1)
            end
        end

        -- reset scissor
        love.graphics.setScissor(sx, sy, sw, sh)

        -- line
        love.graphics.setColor(lineColor[1], lineColor[2], lineColor[3], a)
        love.graphics.rectangle("line", ex + 0.5, ey + 0.5, entity.width - 1, entity.height - 1)

        -- reset color
        love.graphics.setColor(r, g, b, oldAlpha)
    end)
    return spr
end

function gravityDreamBlock.sprite(room, entity)
    local sprites = {}
    local spr = mainSprite(room, entity, nil)
    table.insert(sprites, spr)
    if entity.swapType and entity.swapType > 0 then
        helpers.addSwapTrailSprites(sprites, entity)
    end
    return sprites
end

function gravityDreamBlock.nodeSprite(room, entity, node, nodeIndex)
    local sprites = {}
    local spr = mainSprite(room, entity, node)
    table.insert(sprites, spr)
    return sprites
end

function gravityDreamBlock.nodeLimits(room, entity)
    if entity.swapType and entity.swapType > 0 then
        return {1, 1}
    else
        return {0, 0}
    end
end

function gravityDreamBlock.selection(room, entity)
    local nodes = entity.nodes
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes and nodes[1].x or x, nodes and nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return gravityDreamBlock
