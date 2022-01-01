local consts = require("mods").requireFromPlugin("consts")
local drawableSprite = require("structs.drawable_sprite")

local gravityRefill = {
    name = "GravityHelper/GravityRefill",
    depth = -100,
    placements = {
        {
            name = "normal",
            data = {
                charges = 1,
                oneUse = false,
                refillsDash = true,
                refillsStamina = true,
                respawnTime = 2.5,
            }
        },
    },
}

function gravityRefill.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/refill/idle00", entity)
    sprite:setColor(consts.gravityTypes.toggle.color)
    return sprite
end

return gravityRefill
