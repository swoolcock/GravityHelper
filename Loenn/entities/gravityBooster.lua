-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local drawableSprite = require("structs.drawable_sprite")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.normal.index,
    red = false,
    textureDirectory = "",
    showOverlay = true,
    showRipple = true,
})

local gravityBooster = {
    name = "GravityHelper/GravityBooster",
    depth = -8500,
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType(),
    },
    placements = {
        {
            name = "normal",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.normal.index,
            }),
        },
        {
            name = "inverted",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.inverted.index,
            }),
        },
        {
            name = "toggle",
            data = helpers.union(placementData, {
                gravityType = consts.gravityTypes.toggle.index,
            }),
        },
    },
}

function gravityBooster.selection(room, entity)
    return utils.rectangle(entity.x - 10, entity.y - 10, 20, 20)
end

function gravityBooster.sprite(room, entity)
    local gravityType = entity.gravityType
    local basePath = "objects/GravityHelper/gravityBooster/"
    if entity.textureDirectory and entity.textureDirectory ~= "" then
        basePath = entity.textureDirectory
    end
    basePath = helpers.ensureSingleTrailingSlash(basePath)

    local spriteTexture = entity.red and basePath.."boosterRedUp00" or basePath.."boosterUp00"
    local overlayTexture = basePath ..
            (gravityType == 2 and "overlayToggle01" or gravityType == 1 and "overlayInvert00" or "overlayNormal00")

    local boosterSprite = helpers.fromTexture(spriteTexture, entity)
    local gravityInfo = consts.gravityTypeForIndex(gravityType)
    local sprites = {}
    if boosterSprite then table.insert(sprites, boosterSprite) end

    if entity.showOverlay ~= false then
        local overlaySprite = drawableSprite.fromTexture(overlayTexture, entity)
        if overlaySprite then table.insert(sprites, overlaySprite) end
    end

    local function createRippleSprite(scaleY)
        local rippleSprite = helpers.fromTexture("objects/GravityHelper/ripple03", entity)
        local offset = scaleY < 0 and 5 or -5
        rippleSprite:addPosition(0, offset)
        rippleSprite:setColor(helpers.parseHexColor(gravityInfo.highlightColor))
        rippleSprite:setScale(1, scaleY)
        return rippleSprite
    end

    if entity.showRipple ~= false then
        if gravityType == 0 or gravityType == 2 then
            table.insert(sprites, createRippleSprite(-1))
        end
        if gravityType == 1 or gravityType == 2 then
            table.insert(sprites, createRippleSprite(1))
        end
    end

    return sprites
end

return gravityBooster
