-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    depth = 8500,
    showRipples = false,
    showParticles = true,
    bloomAlpha = 0.6,
    bloomRadius = 14.0,
    idleAlpha = 1.0,
    turningAlpha = 0.4,
    turnTime = 0.3,
    syncToPlayer = false,
})

local gravityIndicator = {
    name = "GravityHelper/GravityIndicator",
    texture = "objects/GravityHelper/gravityIndicator/arrow00",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "cassette_controller",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
        {
            name = "player_synced",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                syncToPlayer = true,
            }),
        }
    },
}

function gravityIndicator.depth(room, entity)
    return entity.depth
end

function gravityIndicator.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityIndicator
