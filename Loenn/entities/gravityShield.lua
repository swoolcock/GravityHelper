-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    oneUse = false,
    shieldTime = 3.0,
    respawnTime = 2.5,
    textureDirectory = "",
})

local gravityShield = {
    name = "GravityHelper/GravityShield",
    depth = -100,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
    },
}

function gravityShield.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

function gravityShield.texture(room, entity)
    local basePath = "objects/GravityHelper/gravityShield/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    return basePath.."idle00"
end

return gravityShield
