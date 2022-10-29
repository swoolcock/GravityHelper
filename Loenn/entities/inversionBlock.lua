-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    leftGravityType = 2,
    rightGravityType = 2,
    topEnabled = true,
    bottomEnabled = true,
    leftEnabled = false,
    rightEnabled = false,
})

local inversionBlock = {
    name = "GravityHelper/InversionBlock",
    borderColor = colors.White,
    fillColor = colors.Black,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        leftGravityType = consts.fieldInformation.gravityType(0,1,2),
        rightGravityType = consts.fieldInformation.gravityType(0,1,2),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                topEnabled = true,
                bottomEnabled = true,
                leftEnabled = false,
                rightEnabled = false,
            }),
        },
        {
            name = "sides",
            data = helpers.union(placementData, {
                topEnabled = false,
                bottomEnabled = false,
                leftEnabled = true,
                rightEnabled = true,
            }),
        },
    },
}

return inversionBlock
