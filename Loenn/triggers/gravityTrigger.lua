local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    defaultToController = true,
    gravityType = consts.gravityTypes.normal.index,
    momentumMultiplier = 1.0,
    sound = "",
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
})

local gravityTrigger = {
    name = "GravityHelper/GravityTrigger",
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

return gravityTrigger
