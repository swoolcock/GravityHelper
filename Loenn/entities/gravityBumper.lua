-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
})

local gravityBumper = {
    name = "GravityHelper/GravityBumper",
    depth = 0,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData),
        },
    },
}

function gravityBumper.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/Bumper/Idle22", entity)
    local type = consts.gravityTypeForIndex(entity.gravityType)
    sprite:setColor(type.color)
    return sprite
end

return gravityBumper
