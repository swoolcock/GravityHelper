local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    persistent = false,
    arrowOpacity = 0.5,
    fieldOpacity = 0.15,
    particleOpacity = 0.5,
})

local fieldGravityController = {
    name = "GravityHelper/FieldGravityController",
    depth = -8500,
    texture = "objects/GravityHelper/gravityController/icon",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

return fieldGravityController
