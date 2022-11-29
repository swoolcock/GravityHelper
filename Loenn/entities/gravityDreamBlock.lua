-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")
--local colors = require("consts.xna_colors")

local drawableSprite = require("structs.drawable_sprite")

local doubleArrowTexture = "objects/GravityHelper/gravityDreamBlock/doubleArrow"
local doubleArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/doubleArrowSmall"
local upArrowTexture = "objects/GravityHelper/gravityDreamBlock/upArrow"
local upArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/upArrowSmall"
local downArrowTexture = "objects/GravityHelper/gravityDreamBlock/downArrow"
local downArrowSmallTexture = "objects/GravityHelper/gravityDreamBlock/downArrowSmall"

local doubleArrowSprite = drawableSprite.fromTexture(doubleArrowTexture)
local doubleArrowSmallSprite = drawableSprite.fromTexture(doubleArrowSmallTexture)
local upArrowSprite = drawableSprite.fromTexture(upArrowTexture)
local upArrowSmallSprite = drawableSprite.fromTexture(upArrowSmallTexture)
local downArrowSprite = drawableSprite.fromTexture(downArrowTexture)
local downArrowSmallSprite = drawableSprite.fromTexture(downArrowSmallTexture)

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
    fall = false,
    climbFall = true,
    fallUp = false,
})

local gravityDreamBlock = {
    name = "GravityHelper/GravityDreamBlock",
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
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

--[[
function gravityDreamBlock.borderColor(room, entity)
    local gravityColor = consts.gravityTypeForIndex(entity.gravityType).color
    local parsed, r, g, b = utils.parseHexColor(entity.lineColor or "")
    return parsed and {r, g, b, 1} or lightened(gravityColor, 0.4)
end

function gravityDreamBlock.fillColor(room, entity)
    local gravityColor = consts.gravityTypeForIndex(entity.gravityType).color
    local parsed, r, g, b = utils.parseHexColor(entity.backColor or "")
    local color = parsed and {r, g, b, 1} or lightened(gravityColor, 0.4)
    return {color[1] * 0.12, color[2] * 0.12, color[3] * 0.12, 1}
end
]]

function gravityDreamBlock.depth(room, entity)
    return entity.below and 4999 or -11001
end

local function putInside(entity, x, y)
    local right, bottom = entity.x + entity.width, entity.y + entity.height
    if x > right then
        x = x - math.ceil((x - right) / entity.width) * entity.width
    elseif x < entity.x then
        x = x + math.ceil((entity.x - x) / entity.width) * entity.width
    end
    if y > bottom then
        y = y - math.ceil((y - bottom) / entity.height) * entity.height
    elseif y < entity.y then
        y = y + math.ceil((entity.y - y) / entity.height) * entity.height
    end
    return x, y
end

function gravityDreamBlock.draw(room, entity)
    local gravityColor = consts.gravityTypeForIndex(entity.gravityType).color
    local lineColor, backColor, particleColor = lightened(gravityColor, 0.4), lightened(gravityColor, 0.4), gravityColor
    local parsed, r, g, b, a

    parsed, r, g, b = utils.parseHexColor(entity.lineColor or "")
    if parsed then lineColor = {r, g, b} end

    parsed, r, g, b = utils.parseHexColor(entity.backColor or "")
    if parsed then backColor = {r, g, b} end

    parsed, r, g, b = utils.parseHexColor(entity.particleColor or "")
    if parsed then particleColor = {r, g, b} end

    -- fill
    r, g, b, a = love.graphics.getColor()
    love.graphics.setColor(0, 0, 0, 1)
    love.graphics.rectangle("fill", entity.x, entity.y, entity.width, entity.height)
    love.graphics.setColor(backColor[1], backColor[2], backColor[3], 0.12)
    love.graphics.rectangle("fill", entity.x, entity.y, entity.width, entity.height)

    -- set scissor for particles
    local sx, sy, sw, sh = love.graphics.getScissor()
    local x1, y1 = love.graphics.transformPoint(entity.x, entity.y)
    local x2, y2 = love.graphics.transformPoint(entity.x + entity.width, entity.y + entity.height)
    love.graphics.setScissor(x1, y1, x2 - x1, y2 - y1)

    -- find sprites
    local largeSprite = entity.gravityType == 0 and downArrowSprite or entity.gravityType == 1 and upArrowSprite or doubleArrowSprite
    local smallSprite = entity.gravityType == 0 and downArrowSmallSprite or entity.gravityType == 1 and upArrowSmallSprite or doubleArrowSmallSprite

    -- set seed
    math.randomseed(entity._id or 1)
    
    -- render particles
    local particleCount = math.floor((entity.width / 8) * (entity.height / 8) * 0.7)
    for _ = 1,particleCount do
        -- get random values
        local px,py = math.random(0, entity.width), math.random(0, entity.height)
        local lightness = math.random() - 0.25
        local layer = math.random(0, 6)
        -- calculate visuals
        local adjusted = lightened(particleColor, lightness, 0.8)

        px, py = putInside(entity, px, py)

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
    love.graphics.setColor(lineColor[1], lineColor[2], lineColor[3], 1)
    love.graphics.rectangle("line", entity.x + 0.5, entity.y + 0.5, entity.width - 1, entity.height - 1)

    -- reset color
    love.graphics.setColor(r, g, b, a)
end

return gravityDreamBlock
