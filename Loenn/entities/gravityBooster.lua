-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
    red = false,
    useTintedSprites = true,
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
            data = helpers.union(placementData),
        },
    },
}

function gravityBooster.sprite(room, entity)
    local spriteTexture = entity.red and "objects/booster/boosterRed00" or "objects/booster/booster00"
    local sprite = drawableSprite.fromTexture(spriteTexture, entity)

    if entity.useTintedSprites then
        local type = consts.gravityTypeForIndex(entity.gravityType)
        sprite:setColor(type.color)
    end

    return sprite
end

return gravityBooster
