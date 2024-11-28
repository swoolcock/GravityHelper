-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local utils = require("utils")
local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")

local placementData = helpers.createPlacementData('2', {
    charges = 1,
    dashes = -1,
    oneUse = false,
    refillsDash = true,
    refillsStamina = true,
    respawnTime = 2.5,
    legacyRefillBehavior = false,
})

local gravityRefill = {
    name = "GravityHelper/GravityRefill",
    depth = -100,
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
    },
    fieldOrder = {
        "x", "y",
        "charges", "respawnTime",
        "dashes", "refillsDash", "refillsStamina",
        "oneUse", "legacyRefillBehavior",
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

function gravityRefill.texture(room, entity)
    local suffix = ""
    if entity.refillsDash and entity.dashes == 2 then
        suffix = "_two_dash"
    elseif not entity.refillsDash then
        suffix = "_no_dash"
    end

    return "objects/GravityHelper/gravityRefill/idle"..suffix.."00"
end

function gravityRefill.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityRefill
