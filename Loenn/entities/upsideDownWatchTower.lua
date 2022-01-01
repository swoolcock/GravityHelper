local drawableSprite = require("structs.drawable_sprite")

local upsideDownWatchTower = {
    name = "GravityHelper/UpsideDownWatchTower",
    depth = -8500,
    justification = {0.5, 1.0},
    nodeLineRenderType = "line",
    nodeLimits = {0, -1},
    placements = {
        {
            name = "normal",
            data = {
                summit = false,
                onlyY = false,
            },
        },
    },
}

function upsideDownWatchTower.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/lookout/lookout05", entity)
    sprite:setScale(1, -1)
    sprite:addPosition(0, 16)
    return sprite
end

return upsideDownWatchTower
