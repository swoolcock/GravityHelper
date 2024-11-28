-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")
local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")

local placementData = helpers.createPlacementData('2', {
    width = 8,
    height = 8,
    charges = 1,
    dashes = -1,
    oneUse = false,
    refillsDash = true,
    refillsStamina = true,
    respawnTime = 2.5,
    wallAlpha = 0.8,
    legacyRefillBehavior = false,
})

local gravityRefillWall = {
    name = "GravityHelper/GravityRefillWall",
    depth = 100,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        charges = {
            fieldType = "integer",
        },
        dashes = {
            fieldType = "integer",
        },
        respawnTime = {
            fieldType = "number",
        },
        wallAlpha = {
            fieldType = "number",
        },
    },
    fieldOrder = {
        "x", "y",
        "width", "height",
        "charges", "respawnTime",
        "dashes", "refillsDash", "refillsStamina",
        "wallAlpha", "oneUse", "legacyRefillBehavior",
    },
    placements = {
        {
            name = "normal",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData),
        },
        {
            name = "normalSingleUse",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                oneUse = true,
            }),
        },
        {
            name = "twoDash",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                dashes = 2,
                charges = 2,
            }),
        },
        {
            name = "twoDashSingleUse",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                dashes = 2,
                charges = 2,
                oneUse = true,
            }),
        },
        {
            name = "noDash",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                refillsDash = false,
                refillsStamina = false,
            }),
        },
        {
            name = "noDashSingleUse",
            ignoredFields = consts.ignoredFields,
            data = helpers.union(placementData, {
                refillsDash = false,
                refillsStamina = false,
                oneUse = true,
            }),
        },
    },
}

function gravityRefillWall.sprite(room, entity)
    -- get colors
    local borderColor, fillColor
    if entity.dashes == 2 then
        borderColor = colors.Orchid
        fillColor = colors.DarkOrchid
    elseif not entity.refillsDash then
        borderColor = colors.LightSkyBlue
        fillColor = colors.CornflowerBlue
    else
        borderColor = colors.BlueViolet
        fillColor = colors.Indigo
    end
    local alpha = entity.oneUse and 0.4 or 1.0
    borderColor = helpers.colorWithAlpha(borderColor, alpha)
    fillColor = helpers.colorWithAlpha(fillColor, alpha)

    -- get rect sprites
    local rect = utils.rectangle(entity.x, entity.y, entity.width, entity.height)
    local rectDrawable = drawableRectangle.fromRectangle("bordered", rect, fillColor, borderColor)
    local rectSprites = rectDrawable:getDrawableSprite()

    -- get refill texture
    local suffix = ""
    if entity.refillsDash and entity.dashes == 2 then
        suffix = "_two_dash"
    elseif not entity.refillsDash then
        suffix = "_no_dash"
    end
    local refillTexture = "objects/GravityHelper/gravityRefill/idle"..suffix.."00"

    -- get refillSprite
    local refillSprite = drawableSprite.fromTexture(refillTexture, entity)
    refillSprite:addPosition(entity.width / 2, entity.height / 2)

    table.insert(rectSprites, refillSprite)
    return rectSprites
end

return gravityRefillWall
