-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local drawableSprite = require("structs.drawable_sprite")
local helpers = require("mods").requireFromPlugin("helpers")
local consts = require("mods").requireFromPlugin("consts")

local placementData = helpers.createPlacementData('1', {
    summit = false,
    onlyY = false,
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
    local sprite = drawableSprite.fromTexture("objects/lookout/lookout05", entity)
    sprite:setScale(1, -1)
    sprite:addPosition(0, 16)
    return sprite
end

return upsideDownWatchTower
