-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('2', {
    gravityType = consts.gravityTypes.normal.index,
    wobbleRate = 1,
    ignoreCoreMode = false,
    singleUse = false,
    static = false,
    randomizeFrame = true,
})

local gravityBumper = {
    name = "GravityHelper/GravityBumper",
    depth = 0,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
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
            name = "none",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.none.index,
                ignoreCoreMode = true,
            }),
        },
        {
            name = "static_normal",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
                static = true,
                wobbleRate = 0,
            }),
        },
        {
            name = "static_inverted",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
                static = true,
                wobbleRate = 0,
            }),
        },
        {
            name = "static_toggle",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
                static = true,
                wobbleRate = 0,
            }),
        },
        {
            name = "static_none",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.none.index,
                ignoreCoreMode = true,
                static = true,
                wobbleRate = 0,
            }),
        },
    },
}

function gravityBumper.selection(room, entity)
    local nodeRectangles = {}
    local x,y,w,h = -13,-13,26,26
    if entity.nodes ~= nil then
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeRectangles, utils.rectangle(node.x + x, node.y + y, w, h))
        end
    end
    return utils.rectangle(entity.x + x, entity.y + y, w, h), nodeRectangles
end

function gravityBumper.sprite(room, entity)
    local gravityType = entity.gravityType

    if gravityType == consts.gravityTypes.none.index then
        return drawableSprite.fromTexture("objects/Bumper/Idle22", entity)
    end

    local bumperSpriteTexture =
            gravityType == consts.gravityTypes.normal.index and "objects/GravityHelper/gravityBumper/normal00" or
            gravityType == consts.gravityTypes.inverted.index and "objects/GravityHelper/gravityBumper/invert00" or
            gravityType == consts.gravityTypes.toggle.index and "objects/GravityHelper/gravityBumper/toggle00" or nil

    local bumperSprite = drawableSprite.fromTexture(bumperSpriteTexture, entity)
    local sprites = {bumperSprite}
    local gravityInfo = consts.gravityTypeForIndex(gravityType)

    local function createRippleSprite(scaleY)
        local rippleSprite = drawableSprite.fromTexture("objects/GravityHelper/ripple03", entity)
        local offset = scaleY < 0 and 8 or -8
        rippleSprite:addPosition(0, offset)
        rippleSprite:setColor(helpers.parseHexColor(gravityInfo.highlightColor))
        rippleSprite:setScale(1, scaleY)
        return rippleSprite
    end

    if gravityType == 0 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(-1))
    end
    if gravityType == 1 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(1))
    end

    return sprites
end

return gravityBumper
