local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
})

local gravityBooster = {
    name = "GravityHelper/GravityBooster",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType,
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function gravityBooster.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/booster/booster00", entity)
    local type = consts.gravityTypeForIndex(entity.gravityType)
    sprite:setColor(type.color)
    return sprite
end

return gravityBooster
