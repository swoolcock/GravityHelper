-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
})

local disableUpTransitionController = {
    name = "GravityHelper/DisableUpTransitionController",
    depth = -8500,
    texture = "objects/GravityHelper/gravityController/noup",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

return disableUpTransitionController
