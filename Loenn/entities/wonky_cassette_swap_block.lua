local utils = require("utils")
local connectedEntities = require("helpers.connected_entities")
local mods = require("mods")
local quantumMechanics = mods.requireFromPlugin("libraries.quantum_mechanics")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local wonkyCassetteSwapBlock = {}

wonkyCassetteSwapBlock.name = "QuantumMechanics/WonkyCassetteSwapBlock"
wonkyCassetteSwapBlock.minimumSize = { 16, 16 }
wonkyCassetteSwapBlock.nodeLimits = { 1, 1 }
wonkyCassetteSwapBlock.nodeVisibility = "never"
wonkyCassetteSwapBlock.fieldInformation = {
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
wonkyCassetteSwapBlock.placements = {
    name = "swapblock",
    data = {
        width = 16,
        height = 16,
        onAtBeats = "1, 3",
        color = "FFFFFF",
        textureDirectory = "objects/cassetteblock",
        boostFrames = -1,
        controllerIndex = 0,
        noReturn = false,
    }
}

local trailNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    useRealSize = true
}

local function getTrailSprites(x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)
    local sprites = {}

    local drawWidth, drawHeight = math.abs(x - nodeX) + width, math.abs(y - nodeY) + height
    x, y = math.min(x, nodeX), math.min(y, nodeY)

    local frameNinePatch = drawableNinePatch.fromTexture(trailTexture, trailNinePatchOptions, x, y, drawWidth, drawHeight)
    local frameSprites = frameNinePatch:getDrawableSprite()

    local depth = trailDepth or 8999
    local color = trailColor or { 1, 1, 1, 1 }
    for _, sprite in ipairs(frameSprites) do
        sprite.depth = depth
        sprite:setColor(color)

        table.insert(sprites, sprite)
    end

    return sprites
end

local function addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)
    for _, sprite in ipairs(getTrailSprites(x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)) do
        table.insert(sprites, sprite)
    end
end

local function getBlockSprites(room, entity)
    local sprites = quantumMechanics.getCassetteBlockSprites(room, entity, true)

    local color = entity.color or "FFFFFF"

    if entity.noReturn then
        local cross = drawableSprite.fromTexture("objects/QuantumMechanics/x", entity)
        cross:addPosition(math.floor(entity.width / 2), math.floor(entity.height / 2))
        cross:setColor(color)
        cross.depth = -11

        table.insert(sprites, cross)
    end

    return sprites
end

function wonkyCassetteSwapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    local sprites = getBlockSprites(room, entity)

    local color = entity.color
    addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, "objects/swapblock/target", color)

    return sprites
end

function wonkyCassetteSwapBlock.nodeSprite(room, entity)
    local sprites = getBlockSprites(room, entity)

    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y

    for _, sprite in ipairs(sprites) do
        sprite:addPosition(nodeX - x, nodeY - y)
    end

    return sprites
end

function wonkyCassetteSwapBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    return utils.rectangle(x, y, width, height), { utils.rectangle(nodeX, nodeY, width, height) }
end

return wonkyCassetteSwapBlock
