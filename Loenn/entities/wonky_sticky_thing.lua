local utils = require("utils")

local wonkyStickyThing = {}

wonkyStickyThing.name = "QuantumMechanics/WonkyStickyThing"
wonkyStickyThing.minimumSize = {8, 8}
wonkyStickyThing.depth = 0
wonkyStickyThing.fieldInformation = {
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
    controllerIndex = {
        fieldType = "integer",
        minimumValue = 0
    },
    color = {
        fieldType = "color",
    }
}
wonkyStickyThing.placements = {
    name = "sticky_thing",
    data = {
        width = 8,
        height = 8,
        onAtBeats = "1, 3",
        controllerIndex = 0,
        color = "FFFFFF"
    }
}

function wonkyStickyThing.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width or 8, entity.height or 8)
end

function wonkyStickyThing.color(room, entity)
    local color = utils.getColor(entity.color or "FFFFFF")
    color[4] = 0.8

    return color
end

return wonkyStickyThing
