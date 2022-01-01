local consts = require("mods").requireFromPlugin("consts")
local drawableSprite = require("structs.drawable_sprite")

local gravityBumper = {
    name = "GravityHelper/GravityBumper",
    depth = 0,
    placements = {
        {
            name = "normal",
            data = {
                gravityType = consts.gravityTypes.normal.index,
            },
        },
    },
}

function gravityBumper.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/Bumper/Idle22", entity)
    local type = consts:gravityTypeForIndex(entity.gravityType)
    sprite:setColor(type.color)
    return sprite
end

return gravityBumper
