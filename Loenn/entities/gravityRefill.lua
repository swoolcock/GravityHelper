-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    charges = 1,
    oneUse = false,
    refillsDash = true,
    refillsStamina = true,
    respawnTime = 2.5,
})

local gravityRefill = {
    name = "GravityHelper/GravityRefill",
    depth = -100,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
        {
            name = "noDash",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                refillsDash = false,
                refillsStamina = false,
            }),
        },
    },
}

function gravityRefill.texture(room, entity)
    local suffix = entity.refillsDash and "" or "_no_dash"
    return "objects/GravityHelper/gravityRefill/idle"..suffix.."00"
end

function gravityRefill.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityRefill
