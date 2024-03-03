-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
})

local holdableSpawnGravityTrigger = {
    name = "GravityHelper/HoldableSpawnGravityTrigger",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "inverted",
            data = helpers.union(placementData),
        },
    },
}

return holdableSpawnGravityTrigger
