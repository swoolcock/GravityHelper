local consts = require("mods").requireFromPlugin("consts")

local helpers = {}

function helpers.union(...)
    local tbl = {}
    local source = {...}
    for _,t in ipairs(source) do
        for k,v in pairs(t) do tbl[k] = v end
    end
    return tbl
end

function helpers.colorWithAlpha(color, alpha)
    return { color[1], color[2], color[3], alpha }
end

function helpers.createPlacementData(pluginVersion, data)
    return helpers.union({
        modVersion = consts.modVersion,
        pluginVersion = pluginVersion,
    }, data)
end

return helpers