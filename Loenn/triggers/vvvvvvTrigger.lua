local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    enable = true,
    onlyOnSpawn = false,
})

local vvvvvvTrigger = {
    name = "GravityHelper/VvvvvvTrigger",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

return vvvvvvTrigger
