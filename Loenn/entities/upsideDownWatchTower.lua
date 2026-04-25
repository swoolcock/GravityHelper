-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local drawableSprite = require("structs.drawable_sprite")
local helpers = require("mods").requireFromPlugin("helpers")
local consts = require("mods").requireFromPlugin("consts")

local placementData = helpers.createPlacementData('1', {
    summit = false,
    onlyY = false,
    textureDirectory = "",
})

local upsideDownWatchTower = {
    name = "GravityHelper/UpsideDownWatchTower",
    depth = -8500,
    justification = {0.5, 1.0},
    nodeLineRenderType = "line",
    nodeLimits = {0, -1},
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function upsideDownWatchTower.sprite(room, entity)
    local basePath = "objects/lookout/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    basePath = helpers.ensureSingleTrailingSlash(basePath)

    local sprite = helpers.fromTexture(basePath.."lookout05", entity)
    if sprite then
        sprite:setScale(1, -1)
        sprite:addPosition(0, 16)
    end
    return sprite
end

function upsideDownWatchTower.selection(room, entity)
    local w,h = 13,16
    return utils.rectangle(entity.x - math.ceil(w/2), entity.y, w, h)
end

return upsideDownWatchTower
