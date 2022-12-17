-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    defaultToController = true,
    gravityType = consts.gravityTypes.normal.index,
    exitGravityType = consts.gravityTypes.none.index,
    attachToSolids = false,
    arrowType = consts.gravityTypes.default.index,
    fieldType = consts.gravityTypes.default.index,
    singleUse = false,
    singleUseSound = "event:/new_content/game/10_farewell/glider_emancipate",
    sound = "",
    arrowOpacity = 0.5,
    fieldOpacity = 0.15,
    particleOpacity = 0.5,
    arrowColor = "FFFFFF",
    fieldColor = "",
    particleColor = "FFFFFF",
    flashOnTrigger = true,
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
    momentumMultiplier = 1.0,
    cassetteIndex = -1,
})

local gravityField = {
    name = "GravityHelper/GravityField",
    depth = -8500,
    borderColor = colors.White,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
        exitGravityType = consts.fieldInformation.gravityType(0,1,2,-1),
        arrowType = consts.fieldInformation.gravityType(0,1,2,-1,-2),
        fieldType = consts.fieldInformation.gravityType(0,1,2,-1,-2),
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
        {
            name = "attachedIndicator",
            data = helpers.union(placementData, {
                defaultToController = false,
                attachToSolids = true,
                arrowOpacity = 1,
                fieldOpacity = 0,
                particleOpacity = 0,
                fieldType = consts.gravityTypes.none.index,
                affectsPlayer = false,
            }),
        },
        {
            name = "visualOnly",
            data = helpers.union(placementData, {
                affectsPlayer = false,
            }),
        },
        {
            name = "cassetteBlock0",
            data = helpers.union(placementData, {
                gravityType = 0,
                cassetteIndex = 0,
                fieldColor = "49aaf0",
            })
        },
        {
            name = "cassetteBlock1",
            data = helpers.union(placementData, {
                gravityType = 1,
                cassetteIndex = 1,
                fieldColor = "f049be",
            })
        },
        {
            name = "cassetteBlock2",
            data = helpers.union(placementData, {
                gravityType = 0,
                cassetteIndex = 2,
                fieldColor = "fcdc3a",
            })
        },
        {
            name = "cassetteBlock3",
            data = helpers.union(placementData, {
                gravityType = 1,
                cassetteIndex = 3,
                fieldColor = "38e04e",
            })
        },
    },
}

function gravityField.fillColor(room, entity)
    local type = consts.gravityTypeForIndex(entity.gravityType)
    return helpers.colorWithAlpha(type.color, 0.5)
end

return gravityField
