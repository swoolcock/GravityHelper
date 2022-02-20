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
    texture = "objects/GravityHelper/gravityRefill/idle00",
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
    },
}

function gravityRefill.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityRefill
