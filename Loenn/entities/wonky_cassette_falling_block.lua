local utils = require("utils")
local mods = require("mods")
local quantumMechanics = mods.requireFromPlugin("libraries.quantum_mechanics")

local wonkyCassetteFallingBlock = {}

wonkyCassetteFallingBlock.name = "QuantumMechanics/WonkyCassetteFallingBlock"
wonkyCassetteFallingBlock.minimumSize = { 16, 16 }
wonkyCassetteFallingBlock.fieldInformation = {
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
wonkyCassetteFallingBlock.placements = {
    name = "fallingblock",
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

function wonkyCassetteFallingBlock.sprite(room, entity)
    return quantumMechanics.getCassetteBlockSprites(room, entity, false)
end

return wonkyCassetteFallingBlock
