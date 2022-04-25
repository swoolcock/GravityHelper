local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = true,
    fieldArrowOpacity = 0.5,
    fieldBackgroundOpacity = 0.15,
    fieldParticleOpacity = 0.5,
    fieldNormalColor="0000FF",
    fieldInvertedColor="FF0000",
    fieldToggleColor="800080",
    fieldArrowColor="FFFFFF",
    fieldParticleColor="FFFFFF",
    lineMinAlpha = 0.45,
    lineMaxAlpha = 0.95,
    lineFlashTime = 0.35,
    lineColor="FFFFFF",
})

local visualGravityController = {
    name = "GravityHelper/VisualGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function visualGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/circle_dot" or "objects/GravityHelper/gravityController/circle"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/field", entity)
    return {iconSprite, typeSprite}
end

return visualGravityController
