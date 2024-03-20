module QuantumMechanicsWonkyCassetteBlockController

using ..Ahorn, Maple

@mapdef Entity "QuantumMechanics/WonkyCassetteBlockController" WonkyCassetteBlockController(
    x::Integer,
    y::Integer,
    bpm::Integer=90,
    bars::Integer=16,
    introBars::Integer=0,
    timeSignature::String="4/4",
    sixteenthNoteParam="sixteenth_note",
    cassetteOffset::Float64=0.0,
    boostFrames::Integer=1,
    disableFlag::String="",
)

const placements = Ahorn.PlacementDict(
    "Wonky Cassette Block Controller (Quantum Mechanics)" => Ahorn.EntityPlacement(
        WonkyCassetteBlockController,
    )
)

const sprite = "objects/QuantumMechanics/wonkyCassetteBlockController"
const color = (1.0, 1.0, 1.0, 1.0)

function Ahorn.selection(entity::WonkyCassetteBlockController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WonkyCassetteBlockController)
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