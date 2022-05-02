local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
    lockCamera = true,
    canSkip = false,
    nodeGravityTypes = "",
})

local gravityBadelineBoost = {
    name = "GravityHelper/GravityBadelineBoost",
    depth = -1000000,
    nodeLineRenderType = "line",
    texture = "objects/badelineboost/idle00",
    nodeLimits = {0, -1},
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

return gravityBadelineBoost
