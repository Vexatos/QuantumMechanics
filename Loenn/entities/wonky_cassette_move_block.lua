local utils = require("utils")
local mods = require("mods")
local quantumMechanics = mods.requireFromPlugin("libraries.quantum_mechanics")
local enums = require("consts.celeste_enums")
local drawableSprite = require("structs.drawable_sprite")

local wonkyCassetteMoveBlock = {}

wonkyCassetteMoveBlock.name = "QuantumMechanics/WonkyCassetteMoveBlock"
wonkyCassetteMoveBlock.minimumSize = { 16, 16 }

local moveSpeeds = {
    ["Slow"] = 60.0,
    ["Fast"] = 75.0
}

wonkyCassetteMoveBlock.fieldInformation = {
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
    },
    direction = {
        options = enums.move_block_directions,
        editable = false
    },
    moveSpeed = {
        options = moveSpeeds,
        minimumValue = 0.0
    },
}
wonkyCassetteMoveBlock.placements = {
    name = "moveblock",
    data = {
        width = 16,
        height = 16,
        onAtBeats = "1, 3",
        color = "FFFFFF",
        textureDirectory = "objects/cassetteblock",
        boostFrames = -1,
        controllerIndex = 0,
        direction = "Right",
        moveSpeed = 60.0,
    }
}

local arrowTextures = {
    up = "objects/CommunalHelper/cassetteMoveBlock/arrow02",
    left = "objects/CommunalHelper/cassetteMoveBlock/arrow04",
    right = "objects/CommunalHelper/cassetteMoveBlock/arrow00",
    down = "objects/CommunalHelper/cassetteMoveBlock/arrow06"
}

function wonkyCassetteMoveBlock.sprite(room, entity)
    local sprites = quantumMechanics.getCassetteBlockSprites(room, entity, true)

    local width, height = entity.width or 16, entity.height or 16
    local color = entity.color or "FFFFFF"

    local direction = string.lower(entity.direction)
    local arrowTexture = arrowTextures[direction] or arrowTextures["right"]

    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    arrowSprite:setColor(color)
    arrowSprite.depth = -11

    table.insert(sprites, arrowSprite)

    return sprites
end

return wonkyCassetteMoveBlock
