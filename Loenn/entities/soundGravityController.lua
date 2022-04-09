local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    persistent = false,
    normalSound = consts.gravityTypes.normal.sound,
    invertedSound = consts.gravityTypes.inverted.sound,
    toggleSound = consts.gravityTypes.toggle.sound,
    musicParam = "",
})

local soundGravityController = {
    name = "GravityHelper/SoundGravityController",
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

return soundGravityController
