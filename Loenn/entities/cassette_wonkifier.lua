local drawableSprite = require("structs.drawable_sprite")

local cassetteWonkifier = {}

local colors = {
    {73 / 255, 170 / 255, 240 / 255},
    {240 / 255, 73 / 255, 190 / 255},
    {252 / 255, 220 / 255, 58 / 255},
    {56 / 255, 224 / 255, 78 / 255},
}

local colorNames = {
    ["Blue"] = 0,
    ["Rose"] = 1,
    ["Bright Sun"] = 2,
    ["Malachite"] = 3
}

cassetteWonkifier.name = "QuantumMechanics/CassetteWonkifier"
cassetteWonkifier.depth = 0
cassetteWonkifier.fieldInformation = {
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
    cassetteIndex = {
        fieldType = "integer",
        options = colorNames,
        editable = false
    },
    controllerIndex = {
        fieldType = "integer",
        minimumValue = 0
    }
}

cassetteWonkifier.placements = {}

for i in ipairs(colors) do
    cassetteWonkifier.placements[i] = {
        name = string.format("wonkifier_%s", i - 1),
        data = {
            onAtBeats = "1, 3",
            cassetteIndex = i - 1,
            controllerIndex = 0,
            freezeUpdate = false
        }
    }
end

function cassetteWonkifier.sprite(room, entity)
    local baseSprite = drawableSprite.fromTexture("objects/QuantumMechanics/cassetteWonkifierBase", entity)

    local index = entity.cassetteIndex or 0
    local color = colors[index + 1] or colors[1]

    local topSprite = drawableSprite.fromTexture("objects/QuantumMechanics/cassetteWonkifierTop", entity)

    topSprite:setColor(color)

    local sprites = {topSprite, baseSprite}

    return sprites
end

return cassetteWonkifier
