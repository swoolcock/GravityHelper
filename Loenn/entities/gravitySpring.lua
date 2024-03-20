-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local drawableSprite = require("structs.drawable_sprite")
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
    indicatorTexture = "",
    spriteName = "",
    overlaySpriteName = "",
    refillDashCount = -1,
    refillStamina = true,
    showOverlay = true,
    refillSound = "event:/new_content/game/10_farewell/pinkdiamond_touch",
})

local function springSprite(entity, rotation)
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

local function makeSpring(name, rotation, xOffset, yOffset, width, height, gravityType)
    return {
        name = name,
        depth = -8501,
        ignoredFields = consts.ignoredFields,
        fieldInformation = {
            gravityType = consts.fieldInformation.gravityType(0,1,2,-1),
            refillDashCount = {
                fieldType = "integer",
            },
        },
        selection = function(room, entity)
            return utils.rectangle(entity.x + xOffset, entity.y + yOffset, width, height)
        end,
        sprite = function(room, entity)
            return springSprite(entity, rotation)
        end,
        placements = {
            {
                name = "normal",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                }),
            },
            {
                name = "twoDash",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                    refillDashCount = 2,
                }),
            },
            {
                name = "noDash",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                    refillDashCount = 0,
                }),
            },
            {
                name = "noStamina",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                    refillStamina = false,
                    refillDashCount = 0,
                }),
            },
            {
                name = "withIndicator",
                data = helpers.union(placementData, {
                    gravityType = gravityType,
                    showIndicator = true,
                }),
            },
        },
    }
end

local gravitySprings = {
    makeSpring("GravityHelper/GravitySpringFloor",
            0, -6, -3, 12, 3,
            consts.gravityTypes.normal.index),
    makeSpring("GravityHelper/GravitySpringWallLeft",
            math.pi / 2, 0, -6, 3, 12,
            consts.gravityTypes.toggle.index),
    makeSpring("GravityHelper/GravitySpringWallRight",
            -math.pi / 2, -3, -6, 3, 12,
            consts.gravityTypes.toggle.index),
    makeSpring("GravityHelper/GravitySpringCeiling",
            math.pi, -6, 0, 12, 3,
            consts.gravityTypes.inverted.index),
}

return gravitySprings
