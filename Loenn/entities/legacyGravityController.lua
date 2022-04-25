local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    alwaysTrigger = false,
    normalGravitySound = consts.gravityTypes.normal.sound,
    invertedGravitySound = consts.gravityTypes.inverted.sound,
    toggleGravitySound = consts.gravityTypes.toggle.sound,
    arrowOpacity = 0.5,
    fieldOpacity = 0.15,
    particleOpacity = 0.5,
    holdableResetTime = 2.0,
    gravityMusicParam = "flip",
    vvvvvv = false,
    vvvvvv_sound = "event:/gravityhelper/toggle",
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
        {
            name = "vvvvvv",
            data = helpers.union(placementData, { vvvvvv = true })
        },
    },
}

return gravityController
