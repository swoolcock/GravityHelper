-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    showRipples = true,
    depth = 8500,
})

local gravityIndicator = {
    name = "GravityHelper/GravityIndicator",
    texture = "objects/GravityHelper/gravityIndicator/idle00",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
    },
}

function gravityIndicator.depth(room, entity)
    return entity.depth
end

function gravityIndicator.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityIndicator
