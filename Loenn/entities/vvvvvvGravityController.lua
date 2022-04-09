local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    persistent = false,
    mode = 2, -- On
    flipSound = "event:/gravityhelper/toggle",
    disableGrab = true,
    disableDash = true,
})

local gravityController = {
    name = "GravityHelper/GravityController",
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

return gravityController
