module QuantumMechanicsWonkyMinorCassetteBlockController

using ..Ahorn, Maple

@mapdef Entity "QuantumMechanics/WonkyMinorCassetteBlockController" WonkyMinorCassetteBlockController(
    x::Integer,
    y::Integer,
    timeSignature::String="4/4",
    controllerIndex::Integer=1,
)

const placements = Ahorn.PlacementDict(
    "Wonky Minor Cassette Block Controller (Quantum Mechanics)" => Ahorn.EntityPlacement(
        WonkyMinorCassetteBlockController,
    ),
)

const sprite = "objects/QuantumMechanics/wonkyMinorCassetteBlockController"
const color = (1.0, 1.0, 1.0, 1.0)

function Ahorn.selection(entity::WonkyMinorCassetteBlockController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WonkyMinorCassetteBlockController)
    Ahorn.drawSprite(ctx, sprite, 0, 0)

    timeSignature = String(get(entity.data, "timeSignature", "4/4"))
    values = split(replace(timeSignature, r"\s" => ""), "/")

    if length(values) == 2 && 0 < length(values[1]) <= 2 && 0 < length(values[2]) <= 2
        Ahorn.drawCenteredText(ctx, String(values[1]), -8, -5, 8, 5, tint=color)
        Ahorn.drawCenteredText(ctx, String(values[2]), -8, 1, 8, 5, tint=color)
    else 
        Ahorn.drawCenteredText(ctx, "e", -8, -5, 8, 5, tint=color) # "e" for error/invalid
    end
end

end