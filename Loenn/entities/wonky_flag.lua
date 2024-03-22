local wonkyFlag = {}

wonkyFlag.name = "QuantumMechanics/WonkyFlag"
wonkyFlag.depth = 0
wonkyFlag.texture = "objects/QuantumMechanics/wonkyFlag"
wonkyFlag.fieldInformation = {
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
    }
}
wonkyFlag.placements = {
    name = "flag",
    data = {
        flag = "",
        onAtBeats = "1, 3",
        controllerIndex = 0,
        invert = false
    }
}

return wonkyFlag
