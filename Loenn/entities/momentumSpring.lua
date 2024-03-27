-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")

local placementData = helpers.createPlacementData('1', {
    sprite = "objects/GravityHelper/springGreen",
    orientation = "Floor",
})

local momentumSpring = {
    name = "GravityHelper/MomentumSpring",
    texture = "objects/GravityHelper/springGreen/00",
    justification = { 0.5, 1.0 },
    depth = -8501,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        orientation = consts.fieldInformation.orientation,
    },
    placements = {
        {
            name = "floor",
            data = helpers.union(placementData, {
                orientation = "Floor",
            })
        },
        {
            name = "ceiling",
            data = helpers.union(placementData, {
                orientation = "Ceiling",
            })
        },
        {
            name = "wallleft",
            data = helpers.union(placementData, {
                orientation = "WallLeft",
            })
        },
        {
            name = "wallright",
            data = helpers.union(placementData, {
                orientation = "WallRight",
            })
        }
    }
}

local orientations = {
    "Floor",
    "WallLeft",
    "Ceiling",
    "WallRight",
}

local function transformsForOrientation(orientation)
    if orientation == orientations[1] then
        return 0, -6, -3, 12, 3
    elseif orientation == orientations[2] then
        return math.pi / 2, 0, -6, 3, 12
    elseif orientation == orientations[3] then
        return math.pi, -6, 0, 12, 3
    elseif orientation == orientations[4] then
        return -math.pi / 2, -3, -6, 3, 12
    end
end

function momentumSpring.rotation(room, entity)
    local rotation = transformsForOrientation(entity.orientation)
    return rotation
end

function momentumSpring.selection(room, entity)
    local _, xOffset, yOffset, width, height = transformsForOrientation(entity.orientation)
    return utils.rectangle(entity.x + xOffset, entity.y + yOffset, width, height)
end

function momentumSpring.flip(room, entity, horizontal, vertical)
    local nameIndex = 0
    for i = 1,4 do
        if entity.orientation == orientations[i] then
            nameIndex = i - 1
            break
        end
    end

    if nameIndex % 2 == 0 and vertical or nameIndex % 2 == 1 and horizontal then
        nameIndex = (nameIndex + 2) % 4
    else
        return false
    end

    entity.orientation = orientations[nameIndex + 1]

    return true
end

function momentumSpring.rotate(room, entity, direction)
    local nameIndex = 0
    for i = 1,4 do
        if entity.orientation == orientations[i] then
            nameIndex = i - 1
            break
        end
    end

    nameIndex = (nameIndex + direction + 4) % 4
    entity.orientation = orientations[nameIndex + 1]

    return true
end

return momentumSpring
