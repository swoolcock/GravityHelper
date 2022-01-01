local consts = require("mods").requireFromPlugin("consts")

local gravityTrigger = {
    name = "GravityHelper/GravityTrigger",
    placements = {
        {
            name = "normal",
            data = {
                width = 8,
                height = 8,
                gravityType = consts.gravityTypes.normal.index,
                momentumMultiplier = 1.0,
                affectsPlayer = true,
                affectsHoldableActors = false,
                affectsOtherActors = false,
            },
        },
    },
}

return gravityTrigger
