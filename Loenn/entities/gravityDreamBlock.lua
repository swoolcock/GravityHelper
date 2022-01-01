local consts = require("mods").requireFromPlugin("consts")
local utils = require("utils")
local colors = require("consts.xna_colors")

local placementData = {
    width = 8,
    height = 8,
    fastMoving = false,
    oneUse = false,
    below = false,
    gravityType = consts.gravityTypes.normal.index,
}

local gravityDreamBlock = {
    name = "GravityHelper/GravityDreamBlock",
    borderColor = colors.White,
    --nodeLimits = {0, 1},
    --nodeLineRenderType = "line",
    placements = {
        {
            name = "normal",
            data = placementData,
        },
        --{
        --    name = "gravityDreamBlock_moving",
        --    data = placementData,
        --},
    },
}

function gravityDreamBlock.fillColor(room, entity)
    local type = consts:gravityTypeForIndex(entity.gravityType)
    return type.color
end

function gravityDreamBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

return gravityDreamBlock
