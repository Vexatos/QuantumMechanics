local drawing = require("utils.drawing")
local drawableFunction = require("structs.drawable_function")
local drawableSprite = require("structs.drawable_sprite")

local wonkyCassetteBlockController = {}

wonkyCassetteBlockController.name = "QuantumMechanics/WonkyCassetteBlockController"
wonkyCassetteBlockController.depth = 0
wonkyCassetteBlockController.fieldInformation = {
    bpm =  {
        fieldType = "integer",
        minimumValue = 1
    },
    bars =  {
        fieldType = "integer",
        minimumValue = 1
    },
    introBars =  {
        fieldType = "integer",
        minimumValue = 0
    },
    timeSignature =  {
        fieldType = "string",
        validator = function(str)
            local values = string.split(str, "/")()
            return #values == 2 and string.match(values[1], "^%d+$") and string.match(values[2], "^%d+$")
        end
    },
    boostFrames = {
        fieldType = "integer",
        minimumValue = 1
    }
}
wonkyCassetteBlockController.placements = {
    name = "controller",
    data = {
        bpm = 90,
        bars = 16,
        introBars = 0,
        timeSignature = "4/4",
        sixteenthNoteParam = "sixteenth_note",
        cassetteOffset = 0.0,
        boostFrames = 1,
        disableFlag = ""
    }
}

local spritePath = "objects/QuantumMechanics/wonkyCassetteBlockController"

local function drawTimeSignature(entity, values)
    drawing.printCenteredText(values[1], entity.x - 9, entity.y - 6, 8, 5)
    drawing.printCenteredText(values[2], entity.x - 9, entity.y, 8, 5)
end

local function drawError(entity)
    drawing.printCenteredText("e", entity.x - 8, entity.y - 5, 8, 5)
end

function wonkyCassetteBlockController.sprite(room, entity)
    local sprite = drawableSprite.fromTexture(spritePath, entity)

    local sprites = {sprite}

    local timeSignature = entity.timeSignature or "4/4"
    local values = string.split(timeSignature, "/")()

    if #values == 2 and 0 < #values[1] and #values[1] <= 2 and 0 < #values[2] and #values[2] <= 2 then
        local signature = drawableFunction.fromFunction(drawTimeSignature, entity, values)
        table.insert(sprites, signature)
    else
        local signature = drawableFunction.fromFunction(drawError, entity)
        table.insert(sprites, signature)
    end
    
    return sprites
end

return wonkyCassetteBlockController
