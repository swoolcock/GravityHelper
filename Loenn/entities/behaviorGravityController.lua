local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = true,
    holdableResetTime = 2.0,
    springCooldown = 1.0,
})

local behaviorGravityController = {
    name = "GravityHelper/BehaviorGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function behaviorGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/icon_dot" or "objects/GravityHelper/gravityController/icon"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/wrench", entity)
    return {iconSprite, typeSprite}
end

return behaviorGravityController
