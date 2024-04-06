-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    enable = true,
    onlyOnSpawn = false,
    enableFlag = "",
})

local vvvvvvTrigger = {
    name = "GravityHelper/VvvvvvTrigger",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "enable",
            data = helpers.union(placementData, {
                enable = true,
            }),
        },
        {
            name = "disable",
            data = helpers.union(placementData, {
                enable = false,
            }),
        },
    },
}

return vvvvvvTrigger
