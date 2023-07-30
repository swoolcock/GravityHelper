-- Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
-- See the LICENCE file in the repository root for full licence text.

local colors = require("consts.xna_colors")

local function makeOptions(options, defaults, ...)
    local requested = {...}
    if #requested == 0 then
        requested = defaults
    end
    local tbl = {}
    for _,v in ipairs(requested) do
        table.insert(tbl, {options[v], v})
    end
    return tbl
end

local consts = {
    modVersion = "1.1.44",
    ignoredFields = {
        "modVersion",
        "pluginVersion",
        "_name",
        "_id",
    },
    fieldInformation = {
        gravityType = function(...)
            return {
                options = makeOptions({
                    [0] = "Normal",
                    [1] = "Inverted",
                    [2] = "Toggle",
                    [-1] = "None",
                    [-2] = "Default",
                }, {0,1,2}, ...)
            }
        end,
        vvvvvvMode = {
            options = {
                {"Trigger-based", 0},
                {"Off", 1},
                {"On", 2},
            },
        },
        fallType = {
            options = {
                {"None", 0},
                {"Down", 1},
                {"Up", 2},
                {"Match Player", 3},
                {"Opposite Player", 4},
            },
        },
    },
    gravityTypes = {
        -- regular gravity
        normal = {
            name = "Normal",
            index = 0,
            color = colors.Blue,
            sound = "event:/ui/game/lookout_off",
            springTexture = "objects/GravityHelper/gravitySpring/normal00",
            switchTexture = "objects/GravityHelper/gravitySwitch/switch12",
        },
        -- inverted gravity
        inverted = {
            name = "Inverted",
            index = 1,
            color = colors.Red,
            sound = "event:/ui/game/lookout_on",
            springTexture = "objects/GravityHelper/gravitySpring/invert00",
            switchTexture = "objects/GravityHelper/gravitySwitch/switch01",
        },
        -- toggle gravity
        toggle = {
            name = "Toggle",
            index = 2,
            color = colors.Purple,
            sound = "",
            springTexture = "objects/GravityHelper/gravitySpring/toggle00",
            switchTexture = "objects/GravityHelper/gravitySwitch/toggle01",
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
