local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    gravityType = consts.gravityTypes.normal.index,
    fireOnBubbleReturn = true,
})

local spawnGravityTrigger = {
    name = "GravityHelper/SpawnGravityTrigger",
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

return spawnGravityTrigger
