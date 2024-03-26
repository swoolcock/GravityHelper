-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")

local placementData = helpers.createPlacementData('2', {
    defaultToController = true,
    playerCanUse = true,
    gravityType = consts.gravityTypes.normal.index,
    gravityCooldown = 0,
    showIndicator = false,
    largeIndicator = false,
    indicatorOffset = 8,
})

local names = {
    "GravityHelper/GravitySpringFloor",
    "GravityHelper/GravitySpringWallLeft",
    "GravityHelper/GravitySpringCeiling",
    "GravityHelper/GravitySpringWallRight",
}

local function flipSpring(room, entity, horizontal, vertical)
    local nameIndex = 0
    for i = 1,4 do
        if entity.name == names[i] then
            nameIndex = i - 1
            break
        end
    end

    if nameIndex % 2 == 0 and vertical or nameIndex % 2 == 1 and horizontal then
        nameIndex = (nameIndex + 2) % 4
    else
        return false
    end

    entity.name = names[nameIndex + 1]

    return true
end

local function rotateSpring(room, entity, direction)
    local nameIndex = 0
    for i = 1,4 do
        if entity.name == names[i] then
            nameIndex = i - 1
            break
        end
    end

    nameIndex = (nameIndex + direction + 4) % 4
    entity.name = names[nameIndex + 1]

    return true
end

local function makeSpring(name, rotation, xOffset, yOffset, width, height, gravityType)
    return {
        name = name,
        rotation = rotation,
        depth = -8501,
        justification = {0.5, 1.0},
        ignoredFields = consts.ignoredFields,
        fieldInformation = {
            gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
        },
        selection = function(room, entity)
            return utils.rectangle(entity.x + xOffset, entity.y + yOffset, width, height)
        end,
        flip = flipSpring,
        rotate = rotateSpring,
        texture = function(room, entity)
            local type = consts.gravityTypeForIndex(entity.gravityType)
            return type.springTexture
        end,
        placements = {
            {
                name = "normal",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                }),
            },
            {
                name = "withIndicator",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                    showIndicator = true,
                }),
            }
        },
    }
end

local gravitySprings = {
    makeSpring(names[1],
            0, -6, -3, 12, 3,
            consts.gravityTypes.normal.index),
    makeSpring(names[2],
            math.pi / 2, 0, -6, 3, 12,
            consts.gravityTypes.toggle.index),
    makeSpring(names[3],
            math.pi, -6, 0, 12, 3,
            consts.gravityTypes.inverted.index),
    makeSpring(names[4],
            -math.pi / 2, -3, -6, 3, 12,
            consts.gravityTypes.toggle.index),
}

return gravitySprings
