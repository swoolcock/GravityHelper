-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")

local placementData = helpers.createPlacementData('1', {
    sprite = "objects/GravityHelper/springGreen",
})

local ceilingMomentumSpring = {
    name = "GravityHelper/CeilingMomentumSpring",
    texture = "objects/GravityHelper/springGreen/00",
    rotation = math.pi,
    justification = { 0.5, 1.0 },
    depth = -8501,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function ceilingMomentumSpring.selection(room, entity)
    return utils.rectangle(entity.x - 6, entity.y, 12, 3)
end

return ceilingMomentumSpring
