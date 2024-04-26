local drawableSprite = require("structs.drawable_sprite")
local connectedEntities = require("helpers.connected_entities")

local quantumMechanics = {}

function quantumMechanics.getTileSprite(entity, x, y, frame, color, depth, rectangles)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = -1, -1

    if completelyClosed then
        if not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 0
        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 8
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 16
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 24
        else
            quadX, quadY = 8, 8
        end
    else
        if closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 8, 0
        elseif closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 8, 16
        elseif closedLeft and not closedRight and closedUp and closedDown then
            quadX, quadY = 16, 8
        elseif not closedLeft and closedRight and closedUp and closedDown then
            quadX, quadY = 0, 8
        elseif closedLeft and not closedRight and not closedUp and closedDown then
            quadX, quadY = 16, 0
        elseif not closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 0, 0
        elseif not closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 0, 16
        elseif closedLeft and not closedRight and closedUp and not closedDown then
            quadX, quadY = 16, 16
        end
    end

    if quadX and quadY then
        local sprite = drawableSprite.fromTexture(frame, entity)

        sprite:addPosition(drawX, drawY)
        sprite:useRelativeQuad(quadX, quadY, 8, 8)
        sprite:setColor(color)

        sprite.depth = depth

        return sprite
    end
end

return quantumMechanics
