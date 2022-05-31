-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
    red = false,
})

local gravityBooster = {
    name = "GravityHelper/GravityBooster",
    depth = -8500,
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

function gravityBooster.sprite(room, entity)
    local gravityType = entity.gravityType
    local spriteTexture = entity.red and "objects/booster/boosterRed00" or "objects/booster/booster00"
    local overlaySprite = drawableSprite.fromTexture("objects/GravityHelper/gravityBooster/overlay", entity)
    local boosterSprite = drawableSprite.fromTexture(spriteTexture, entity)
    local gravityInfo = consts.gravityTypeForIndex(gravityType)

    overlaySprite:setColor(gravityInfo.color)

    local sprites = {boosterSprite, overlaySprite}

    local function createRippleSprite(scaleY)
        local rippleSprite = drawableSprite.fromTexture("objects/GravityHelper/ripple03", entity)
        local offset = scaleY < 0 and 5 or -5
        rippleSprite:addPosition(0, offset)
        rippleSprite:setColor(gravityInfo.color)
        rippleSprite:setScale(1, scaleY)
        return rippleSprite
    end

    if gravityType == 1 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(1))
    end
    if gravityType == 0 or gravityType == 2 then
        table.insert(sprites, createRippleSprite(-1))
    end

    return sprites
end

return gravityBooster
