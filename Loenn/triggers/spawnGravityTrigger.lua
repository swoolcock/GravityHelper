local consts = require("mods").requireFromPlugin("consts")

local spawnGravityTrigger = {
    name = "GravityHelper/SpawnGravityTrigger",
    placements = {
        {
            name = "normal",
            data = {
                width = 8,
                height = 8,
                gravityType = consts.gravityTypes.normal.index,
                fireOnBubbleReturn = true,
            },
        },
    },
}

return spawnGravityTrigger
