local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")

local placementData = helpers.createPlacementData('1', {
    defaultToController = true,
    playerCanUse = true,
    gravityType = consts.gravityTypes.normal.index,
    gravityCooldown = 0.1,
})

local function makeSpring(name, rotation, xOffset, yOffset, width, height, gravityType)
    return {
        name = name,
        rotation = rotation,
        depth = -8501,
        justification = {0.5, 1.0},
        ignoredFields = consts.ignoredFields,
        fieldInformation = {
            gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
        },
        selection = function(room, entity)
            return utils.rectangle(entity.x + xOffset, entity.y + yOffset, width, height)
        end,
        texture = function(room, entity)
            local type = consts.gravityTypeForIndex(entity.gravityType)
            return type.springTexture
        end,
        placements = {
            name = "normal",
            data = helpers.union(placementData, {
                gravityType = gravityType,
            }),
        },
    }
end

local gravitySprings = {
    makeSpring("GravityHelper/GravitySpringFloor",
            0, -6, -3, 12, 3,
            consts.gravityTypes.normal.index),
    makeSpring("GravityHelper/GravitySpringWallLeft",
            math.pi / 2, 0, -6, 3, 12,
            consts.gravityTypes.toggle.index),
    makeSpring("GravityHelper/GravitySpringWallRight",
            -math.pi / 2, -3, -6, 3, 12,
            consts.gravityTypes.toggle.index),
    makeSpring("GravityHelper/GravitySpringCeiling",
            math.pi, -6, 0, 12, 3,
            consts.gravityTypes.inverted.index),
}

return gravitySprings
