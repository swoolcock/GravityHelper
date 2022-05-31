-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
    lockCamera = true,
    canSkip = false,
    nodeGravityTypes = "",
})

local gravityBadelineBoost = {
    name = "GravityHelper/GravityBadelineBoost",
    depth = -1000000,
    nodeLineRenderType = "line",
    nodeLimits = {0, -1},
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

local function getSprites(room, entity, gravityType, x, y)
    local maskSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityBadelineBoost/mask00", entity)
    local badelineSprite = drawableSprite.fromTexture("objects/badelineboost/idle00", entity)
    local gravityInfo = consts.gravityTypeForIndex(gravityType)

    if x ~= nil and y ~= nil then
        maskSprite:setPosition(x, y)
        badelineSprite:setPosition(x, y)
    end

    maskSprite:setColor(gravityInfo.color)

    local sprites = {maskSprite, badelineSprite}

    local function createRippleSprite(scaleY)
        local rippleSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityBadelineBoost/ripple03", entity)
        local offset = scaleY < 0 and 4 or -3
        if x ~= nil and y ~= nil then
            rippleSprite:setPosition(x, y + offset)
        else
            rippleSprite:addPosition(0, offset)
        end
        rippleSprite:setColor(gravityInfo.color)
        rippleSprite:setScale(1, scaleY)
        return rippleSprite
    end

    if gravityType == 0 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(1))
    end
    if gravityType == 1 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(-1))
    end

    return sprites
end

local function gravityTypeForNode(entity, index)
    local gravityType = entity.gravityType
    local nodeTypesString = entity.nodeGravityTypes

    if nodeTypesString ~= "" then
        local nodeTypes = string.split(nodeTypesString, ",")()
        if index >= 1 and index <= #nodeTypes then
            gravityType = tonumber(nodeTypes[index]) or gravityType
        end
    end

    return math.min(math.max(gravityType, -1), 2)
end

function gravityBadelineBoost.sprite(room, entity)
    return getSprites(room, entity, gravityTypeForNode(entity, 1))
end

function gravityBadelineBoost.nodeSprite(room, entity, node, nodeIndex)
    return getSprites(room, entity, gravityTypeForNode(entity, nodeIndex + 1), node.x, node.y)
end

return gravityBadelineBoost
