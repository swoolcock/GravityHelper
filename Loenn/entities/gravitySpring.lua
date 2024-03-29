-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local drawableSprite = require("structs.drawable_sprite")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local utils = require("utils")

local placementData = helpers.createPlacementData('3', {
    orientation = "Floor",
    defaultToController = true,
    playerCanUse = true,
    gravityType = consts.gravityTypes.normal.index,
    gravityCooldown = 0,
    showIndicator = false,
    largeIndicator = false,
    indicatorOffset = 8,
    indicatorTexture = "",
    spriteName = "",
    overlaySpriteName = "",
    refillDashCount = -1,
    refillStamina = true,
    showOverlay = true,
    refillSound = "event:/new_content/game/10_farewell/pinkdiamond_touch",
})

local gravitySpring = {
    name = "GravityHelper/GravitySpring",
    depth = -8501,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
        refillDashCount = {
            fieldType = "integer",
        },
        orientation = consts.fieldInformation.orientation,
    },
}

local orientations = {
    "Floor",
    "WallLeft",
    "Ceiling",
    "WallRight",
}

local function createAllPlacements()
    local allPlacements = { }
    local function createPlacements(name, gravityType, orientation)
        table.insert(allPlacements, {
            name = name .. "_normal",
            data = helpers.union(placementData, {
                orientation = orientation,
                gravityType = gravityType,
            }),
        })
        table.insert(allPlacements, {
            name = name .. "_twoDash",
            data = helpers.union(placementData, {
                orientation = orientation,
                gravityType = gravityType,
                refillDashCount = 2,
            }),
        })
        table.insert(allPlacements, {
            name = name .. "_noDash",
            data = helpers.union(placementData, {
                orientation = orientation,
                gravityType = gravityType,
                refillDashCount = 0,
            }),
        })
        table.insert(allPlacements, {
            name = name .. "_noStamina",
            data = helpers.union(placementData, {
                orientation = orientation,
                gravityType = gravityType,
                refillDashCount = 0,
                refillStamina = false,
            }),
        })
        table.insert(allPlacements, {
            name = name .. "_withIndicator",
            data = helpers.union(placementData, {
                orientation = orientation,
                gravityType = gravityType,
                showIndicator = true,
            }),
        })
    end

    createPlacements("floor", 0, orientations[1])
    createPlacements("wallleft", 2, orientations[2])
    createPlacements("ceiling", 1, orientations[3])
    createPlacements("wallright", 2, orientations[4])

    return allPlacements
end

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

function gravitySpring.selection(room, entity)
    local _, xOffset, yOffset, width, height = transformsForOrientation(entity.orientation)
    return utils.rectangle(entity.x + xOffset, entity.y + yOffset, width, height)
end

function gravitySpring.sprite(room, entity)
    local sprites = { }
    local spritePath = "objects/GravityHelper/gravitySpring/"
    local textureName = "none00"

    if entity.gravityType == consts.gravityTypes.normal.index then
        textureName = "normal00"
    elseif entity.gravityType == consts.gravityTypes.inverted.index then
        textureName = "invert00"
    elseif entity.gravityType == consts.gravityTypes.toggle.index then
        textureName = "toggle00"
    end

    local rotation = transformsForOrientation(entity.orientation)
    local mainSprite = drawableSprite.fromTexture(spritePath .. textureName, entity)
    mainSprite:setJustification(0.5, 1.0)
    mainSprite.rotation = rotation
    table.insert(sprites, mainSprite)

    local overlayTextureName = ""
    if entity.showOverlay then
        if not entity.refillStamina then
            overlayTextureName = "no_stamina00"
        elseif entity.refillDashCount == 0 then
            overlayTextureName = "no_dash00"
        elseif entity.refillDashCount >= 2 then
            overlayTextureName = "two_dash00"
        end
    end

    if overlayTextureName ~= "" then
        local overlaySprite = drawableSprite.fromTexture(spritePath .. overlayTextureName, entity)
        overlaySprite:setJustification(0.5, 1.0)
        overlaySprite.rotation = rotation
        table.insert(sprites, overlaySprite)
    end

    return sprites
end

function gravitySpring.flip(room, entity, horizontal, vertical)
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

function gravitySpring.rotate(room, entity, direction)
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

gravitySpring.placements = createAllPlacements()

return gravitySpring
