local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")

local placementData = helpers.createPlacementData('1', {
    width = 8,
    height = 8,
    gravityType = consts.gravityTypes.normal.index,
    attachToSolids = false,
    arrowType = consts.gravityTypes.default.index,
    fieldType = consts.gravityTypes.default.index,
    sound = nil,
    arrowOpacity = nil,
    fieldOpacity = nil,
    particleOpacity = nil,
    arrowColor = nil,
    fieldColor = nil,
    particleColor = nil,
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
})

local gravityField = {
    name = "GravityHelper/GravityField",
    depth = -8500,
    borderColor = colors.White,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType,
        arrowType = consts.fieldInformation.gravityType,
        fieldType = consts.fieldInformation.gravityType,
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
