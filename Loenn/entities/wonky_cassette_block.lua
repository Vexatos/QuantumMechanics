local utils = require("utils")
local connectedEntities = require("helpers.connected_entities")
local mods = require("mods")
local quantumMechanics = mods.requireFromPlugin("libraries.quantum_mechanics")

local wonkyCassetteBlock = {}

wonkyCassetteBlock.name = "QuantumMechanics/WonkyCassetteBlock"
wonkyCassetteBlock.minimumSize = { 16, 16 }
wonkyCassetteBlock.fieldInformation = {
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
wonkyCassetteBlock.placements = {
    name = "block",
    data = {
        width = 16,
        height = 16,
        onAtBeats = "1, 3",
        color = "FFFFFF",
        textureDirectory = "objects/cassetteblock",
        boostFrames = -1,
        controllerIndex = 0
    }
}

-- Filter by cassette blocks sharing the same index
local function getSearchPredicate(entity)
    local entityKey = string.gsub(entity.onAtBeats, "%s", "")
    return function(target)
        return entity._name == target._name and entity.controllerIndex == target.controllerIndex and
            entityKey == string.gsub(target.onAtBeats, "%s", "")
    end
end

function wonkyCassetteBlock.sprite(room, entity)
    local relevantBlocks = utils.filter(getSearchPredicate(entity), room.entities)

    connectedEntities.appendIfMissing(relevantBlocks, entity)

    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local sprites = {}

    local width, height = entity.width or 32, entity.height or 32
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local color = entity.color or "FFFFFF"
    local frame = (entity.textureDirectory or "objects/cassetteblock") .. "/solid"
    local depth = -10

    for x = 1, tileWidth do
        for y = 1, tileHeight do
            local sprite = quantumMechanics.getTileSprite(entity, x, y, frame, color, depth, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    return sprites
end

return wonkyCassetteBlock
