local colors = require("consts.xna_colors")

local consts = {
    modVersion = "1.0.40",
    ignoredFields = {
        "modVersion",
        "pluginVersion",
    },
    gravityTypes = {
        -- regular gravity
        normal = {
            name = "Normal",
            index = 0,
            color = colors.Blue,
            sound = "event:/ui/game/lookout_off",
            springTexture = "objects/GravityHelper/gravitySpring/normal00",
        },
        -- inverted gravity
        inverted = {
            name = "Inverted",
            index = 1,
            color = colors.Red,
            sound = "event:/ui/game/lookout_on",
            springTexture = "objects/GravityHelper/gravitySpring/invert00",
        },
        -- toggle gravity
        toggle = {
            name = "Toggle",
            index = 2,
            color = colors.Purple,
            sound = "",
            springTexture = "objects/GravityHelper/gravitySpring/toggle00",
        },
        -- do not affect gravity
        none = {
            name = "None",
            index = -1,
            color = colors.White,
            sound = "",
            springTexture = "objects/GravityHelper/gravitySpring/none00",
        },
        -- use the default setting provided by the controller
        default = {
            name = "Default",
            index = -2,
            color = colors.White,
            sound = "",
            springTexture = "objects/GravityHelper/gravitySpring/none00",
        },
    },
}

function consts.gravityTypeForIndex(index)
    for _,v in pairs(consts.gravityTypes) do
        if v.index == index then return v end
    end
    return nil
end

return consts
