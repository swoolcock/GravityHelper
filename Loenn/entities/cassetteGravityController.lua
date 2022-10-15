-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = true,
    cassetteSequence = "0,1",
    momentumMultiplier = 1.0,
})

local cassetteGravityController = {
    name = "GravityHelper/CassetteGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    placements = {
        {
            name = "single_room",
            data = helpers.union(placementData, {
                persistent = false,
            }),
        },
        {
            name = "persistent",
            data = helpers.union(placementData),
        },
    },
}

function cassetteGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/circle_dot" or "objects/GravityHelper/gravityController/circle"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/wrench", entity)
    return {iconSprite, typeSprite}
end

return cassetteGravityController
