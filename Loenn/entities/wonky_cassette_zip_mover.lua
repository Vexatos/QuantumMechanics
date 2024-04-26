local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")
local connectedEntities = require("helpers.connected_entities")
local drawableLine = require("structs.drawable_line")
local mods = require("mods")
local quantumMechanics = mods.requireFromPlugin("libraries.quantum_mechanics")

local wonkyCassetteZipMover = {}

wonkyCassetteZipMover.name = "QuantumMechanics/WonkyCassetteZipMover"
wonkyCassetteZipMover.minimumSize = { 16, 16 }
wonkyCassetteZipMover.nodeLimits = { 1, -1 }
wonkyCassetteZipMover.nodeVisibility = "never"
wonkyCassetteZipMover.fieldInformation = {
    onAtBeats = {
        fieldType = "string",
        validator = function(str)
            for beat in string.gmatch(str, "([^,]+)") do
                if not string.match(beat, "^%s*%d+$") then
                    return false
                end
            end

            return true
        end
    },
    color = {
        fieldType = "color",
    },
    textureDirectory = {
        fieldType = "path",
        filePickerExtensions = { "png" },
        allowMissingPath = true,
        filenameProcessor = function(filename)
            -- Discard leading "Graphics/Atlases/Gameplay/" and file extension
            local filename, ext = utils.splitExtension(filename)
            local parts = utils.splitpath(filename, "/")

            return utils.convertToUnixPath(utils.joinpath(unpack(parts, 4)))
        end,
        filenameResolver = function(filename, text, prefix)
            return string.format("%s/Graphics/Atlases/Gameplay/%s.png", prefix, text)
        end
    },
    boostFrames = {
        fieldType = "integer",
        minimumValue = -1
    },
    controllerIndex = {
        fieldType = "integer",
        minimumValue = 0
    }
}
wonkyCassetteZipMover.placements = {
    name = "block",
    data = {
        width = 16,
        height = 16,
        onAtBeats = "1, 3",
        color = "FFFFFF",
        textureDirectory = "objects/cassetteblock",
        boostFrames = -1,
        controllerIndex = 0,
        noReturn = false,
        permanent = false,
        waiting = false,
        ticking = false,
    }
}

local ropeColors = {
    { 110 / 255, 189 / 255, 245 / 255, 1.0 },
    { 194 / 255, 116 / 255, 171 / 255, 1.0 },
    { 227 / 255, 214 / 255, 148 / 255, 1.0 },
    { 128 / 255, 224 / 255, 141 / 255, 1.0 }
}

local function addNodeSprites(sprites, cogTexture, cogColor, ropeColor, centerX, centerY, centerNodeX, centerNodeY, depth)
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture)
    nodeCogSprite:setColor(cogColor)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = { centerX, centerY, centerNodeX, centerNodeY }
    local leftLine = drawableLine.fromPoints(points, ropeColor, 1)
    local rightLine = drawableLine.fromPoints(points, ropeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = depth
    rightLine.depth = depth

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

local function getZipMoverNodeSprites(x, y, width, height, nodes, cogTexture, cogColor, ropeColor, pathDepth)
    local sprites = {}

    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    local depth = pathDepth or 5000

    local cx, cy = centerX, centerY
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight
        addNodeSprites(sprites, cogTexture, cogColor, ropeColor, cx, cy, centerNodeX, centerNodeY, depth)
        cx, cy = centerNodeX, centerNodeY
    end

    return sprites
end

function wonkyCassetteZipMover.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 32, entity.height or 32
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local color = entity.color or "FFFFFF"
    local frame = (entity.textureDirectory or "objects/cassetteblock") .. "/solid"
    local depth = -10

    local rectangles = connectedEntities.getEntityRectangles({ entity })

    for x = 1, tileWidth do
        for y = 1, tileHeight do
            local sprite = quantumMechanics.getTileSprite(entity, x, y, frame, color, depth, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    local ropeColor = (entity.customColor ~= "" and color) or ropeColors[1]

    local nodes = entity.nodes or { { x = 0, y = 0 } }
    local nodeSprites = getZipMoverNodeSprites(x, y, width, height, nodes,
        "objects/QuantumMechanics/wonkyCassetteZipMover/cog",
        color, ropeColor)
    for _, sprite in ipairs(nodeSprites) do
        table.insert(sprites, sprite)
    end

    if entity.noReturn then
        local cross = drawableSprite.fromTexture("objects/QuantumMechanics/wonkyCassetteZipMover/x")
        cross:setPosition(centerX, centerY)
        cross:setColor(color)
        cross.depth = -11

        table.insert(sprites, cross)
    end

    return sprites
end

function wonkyCassetteZipMover.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local mainRectangle = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or { { x = 0, y = 0 } }
    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight

        table.insert(nodeRectangles, utils.rectangle(centerNodeX - 5, centerNodeY - 5, 10, 10))
    end

    return mainRectangle, nodeRectangles
end

return wonkyCassetteZipMover
