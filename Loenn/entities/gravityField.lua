local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    defaultToController = true,
    gravityType = consts.gravityTypes.normal.index,
    attachToSolids = false,
    arrowType = consts.gravityTypes.default.index,
    fieldType = consts.gravityTypes.default.index,
    sound = "",
    arrowOpacity = 0.5,
    fieldOpacity = 0.15,
    particleOpacity = 0.5,
    arrowColor = "FFFFFF",
    fieldColor = "",
    particleColor = "FFFFFF",
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
    momentumMultiplier = 1.0,
})

local gravityField = {
    name = "GravityHelper/GravityField",
    depth = -8500,
    borderColor = colors.White,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
        arrowType = consts.fieldInformation.gravityType(0,1,2,-1,-2),
        fieldType = consts.fieldInformation.gravityType(0,1,2,-1,-2),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
        {
            name = "attachedIndicator",
            data = helpers.union(placementData, {
                attachToSolids = true,
                drawArrows = true,
                drawField = false,
                visualOnly = true,
            }),
        },
        {
            name = "visualOnly",
            data = helpers.union(placementData, {
                attachToSolids = false,
                drawArrows = true,
                drawField = true,
                visualOnly = true,
            }),
        },
    },
}

function gravityField.fillColor(room, entity)
    local type = consts.gravityTypeForIndex(entity.gravityType)
    return helpers.colorWithAlpha(type.color, 0.5)
end

return gravityField
