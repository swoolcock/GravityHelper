-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    defaultToController = true,
    gravityType = consts.gravityTypes.normal.index,
    exitGravityType = consts.gravityTypes.none.index,
    momentumMultiplier = 1.0,
    sound = "",
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
    enableFlag = "",
})

local gravityTrigger = {
    name = "GravityHelper/GravityTrigger",
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
        exitGravityType = consts.fieldInformation.gravityType(0,1,2,-1),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
            }),
        },
        {
            name = "inverted",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
            }),
        },
        {
            name = "toggle",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
            }),
        },
    },
}

return gravityTrigger
