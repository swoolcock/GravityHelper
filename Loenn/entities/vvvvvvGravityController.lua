-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    persistent = true,
    mode = 2, -- On
    flipSound = "event:/gravityhelper/toggle",
    disableGrab = true,
    disableDash = true,
    disableWallJump = true,
    solidTilesBehavior = "Flip",
    otherPlatformBehavior = "Flip",
    extraJumpsBehavior = "Flip",
})

local vvvvvvGravityController = {
    name = "GravityHelper/VvvvvvGravityController",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    fieldOrder = {
        "x", "y",
        "flipSound", "mode",
        "solidTilesBehavior", "otherPlatformBehavior",
        "extraJumpsBehavior", "disableDash", "disableGrab",
        "disableWallJump", "persistent"
    },
    fieldInformation = {
        mode = consts.fieldInformation.vvvvvvMode,
        solidTilesBehavior = {
            editable = false,
            options = {
                "Flip",
                "Jump",
                "None",
            }
        },
        otherPlatformBehavior = {
            editable = false,
            options = {
                "Flip",
                "Jump",
                "None",
            }
        },
        extraJumpsBehavior = {
            editable = false,
            options = {
                "Flip",
                "Jump",
                "None",
            }
        },
    },
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

function vvvvvvGravityController.sprite(room, entity)
    local spriteName = entity.persistent and "objects/GravityHelper/gravityController/circle_dot" or "objects/GravityHelper/gravityController/circle"
    local iconSprite = drawableSprite.fromTexture(spriteName, entity)
    local typeSprite = drawableSprite.fromTexture("objects/GravityHelper/gravityController/spikes", entity)
    return {iconSprite, typeSprite}
end

return vvvvvvGravityController
