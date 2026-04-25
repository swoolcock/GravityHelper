-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('1', {
    defaultToController = true,
    gravityType = consts.gravityTypes.toggle.index,
    cooldown = 1.0,
    textureDirectory = "",
})

local gravitySwitch = {
    name = "GravityHelper/GravitySwitch",
    depth = 2000,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(0,1,2),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
            }),
        },
        {
            name = "inverted",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
            }),
        },
        {
            name = "toggle",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
            }),
        },
    },
}

function gravitySwitch.texture(room, entity)
    local basePath = "objects/GravityHelper/gravitySwitch/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    basePath = helpers.ensureSingleTrailingSlash(basePath)
    local type = consts.gravityTypeForIndex(entity.gravityType)
    return basePath..(type.switchTexture)
end

function gravitySwitch.selection(room, entity)
    local w,h = 16,16
    return utils.rectangle(entity.x - w/2, entity.y - h/2 + 1, w, h)
end

return gravitySwitch
