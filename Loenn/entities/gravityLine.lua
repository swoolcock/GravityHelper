local consts = require("mods").requireFromPlugin("consts")
local helpers = require("mods").requireFromPlugin("helpers")
local colors = require("consts.xna_colors")

local utils = require("utils")
local drawableLine = require("structs.drawable_line")

local placementData = helpers.createPlacementData('1', {
    gravityType = consts.gravityTypes.toggle.index,
    momentumMultiplier = 1,
    cooldown = 0,
    cancelDash = false,
    disableUntilExit = false,
    onlyWhileFalling = false,
    affectsPlayer = true,
    affectsHoldableActors = false,
    affectsOtherActors = false,
    playSound = "event:/gravityhelper/gravity_line",
    minAlpha = nil,
    maxAlpha = nil,
    flashTime = nil,
    lineColor = nil,
})

local gravityLine = {
    name = "GravityHelper/GravityLine",
    depth = -8500,
    nodeLineRenderType = "line",
    nodeLimits = {1, 1},
    ignoredFields = consts.ignoredFields,
    fieldInformation = {
        gravityType = consts.fieldInformation.gravityType,
    },
    placements = {
        {
            name = "crossable",
            data = helpers.union(placementData),
        },
        {
            name = "uncrossable",
            data = helpers.union(placementData, {
                momentumMultiplier = 0.1,
                cancelDash = true,
                disableUntilExit = true,
                onlyWhileFalling = true
            }),
        },
    },
}

function gravityLine.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y
    local line = drawableLine.fromPoints({x, y, nodeX, nodeY}, colors.White, 2)

    line.depth = 5000

    for _, sprite in ipairs(line:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    return sprites
end

function gravityLine.selection(room, entity)
    local main = utils.rectangle(entity.x - 2, entity.y - 2, 5, 5)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x - 2, node.y - 2, 5, 5)
        end
    end

    return main, nodes
end

return gravityLine
