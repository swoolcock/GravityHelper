local consts = require("mods").requireFromPlugin("consts")
local drawableSprite = require("structs.drawable_sprite")

local gravityBooster = {
    name = "GravityHelper/GravityBooster",
    depth = -8500,
    placements = {
        {
            name = "normal",
            data = {
                gravityType = consts.gravityTypes.normal.index,
            },
        },
    },
}

function gravityBooster.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/booster/booster00", entity)
    local type = consts:gravityTypeForIndex(entity.gravityType)
    sprite:setColor(type.color)
    return sprite
end

return gravityBooster
