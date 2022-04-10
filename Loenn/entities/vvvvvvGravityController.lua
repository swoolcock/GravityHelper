local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = false,
    mode = 2, -- On
    flipSound = "event:/gravityhelper/toggle",
    disableGrab = true,
    disableDash = true,
})

local vvvvvvGravityController = {
    name = "GravityHelper/VvvvvvGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function vvvvvvGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/icon_dot" or "objects/GravityHelper/gravityController/icon"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/spikes", entity)
    return {iconSprite, typeSprite}
end

return vvvvvvGravityController
