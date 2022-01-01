local helpers = {}

function helpers.union(first, second)
    local tbl = {}
    for k,v in pairs(first) do tbl[k] = v end
    for k,v in pairs(second) do tbl[k] = v end
    return tbl
end

function helpers.colorWithAlpha(color, alpha)
    return { color[1], color[2], color[3], alpha }
end

return helpers