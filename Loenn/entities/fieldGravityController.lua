local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = false,
    arrowOpacity = 0.5,
    fieldOpacity = 0.15,
    particleOpacity = 0.5,
})

local fieldGravityController = {
    name = "GravityHelper/FieldGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function fieldGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/icon_dot" or "objects/GravityHelper/gravityController/icon"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/field", entity)
    return {iconSprite, typeSprite}
end

return fieldGravityController
