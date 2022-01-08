local utils = require("utils")

local gravityRefill = {
    name = "GravityHelper/GravityRefill",
    depth = -100,
    texture = "objects/GravityHelper/gravityRefill/idle00",
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

function gravityRefill.selection(room, entity)
    return utils.rectangle(entity.x - 4, entity.y - 5, 8, 10)
end

return gravityRefill
