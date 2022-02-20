module GravityHelperGravityLine

using ..Ahorn, Maple

@pardef GravityLine(
    x1::Integer, y1::Integer,
    x2::Integer=x1+16, y2::Integer=y1,
    gravityType::Integer=2, momentumMultiplier::Real=1,
    cooldown::Real=0, cancelDash::Bool=false, disableUntilExit::Bool=false, onlyWhileFalling::Bool=false,
    affectsPlayer::Bool=true, affectsHoldableActors::Bool=false, affectsOtherActors::Bool=false,
    playSound::Bool=true
) = Entity("GravityHelper/GravityLine",
    x = x1, y = y1,
    gravityType=gravityType, momentumMultiplier=momentumMultiplier,
    cooldown=cooldown, cancelDash=cancelDash, disableUntilExit=disableUntilExit, onlyWhileFalling=onlyWhileFalling,
    affectsPlayer=affectsPlayer, affectsHoldableActors=affectsHoldableActors, affectsOtherActors=affectsOtherActors,
    playSound=playSound,
    nodes=Tuple{Int, Int}[(x2, y2)]
)

const placements = Ahorn.PlacementDict(
    "Gravity Line (Crossable) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityLine,
        "line",
        Dict{String, Any}(
            "cooldown" => 0,
            "momentumMultiplier" => 1,
            "cancelDash" => false,
            "gravityType" => 2,
            "disableUntilExit" => false,
            "onlyWhileFalling" => false,
        )
    ),
    "Gravity Line (Uncrossable) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityLine,
        "line",
        Dict{String, Any}(
            "cooldown" => 0,
            "momentumMultiplier" => 0.1,
            "cancelDash" => true,
            "gravityType" => 2,
            "disableUntilExit" => true,
            "onlyWhileFalling" => true,
        )
    )
)

const gravityTypes = Dict{String, Integer}(
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    0 => (0.0, 0.0, 1.0, 0.5),
    1 => (1.0, 0.0, 0.0, 0.5),
    2 => (0.5, 0.0, 0.5, 0.5),
)

Ahorn.editingOptions(trigger::GravityLine) = Dict{String, Any}(
    "gravityType" => gravityTypes,
)

Ahorn.nodeLimits(entity::GravityLine) = 1, 1

function Ahorn.selection(entity::GravityLine)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    nx, ny = Int.(nodes[1])
    return [Ahorn.Rectangle(x-4, y-4, 8, 8), Ahorn.Rectangle(nx-4, ny-4, 8, 8)]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityLine)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    nx, ny = Int.(nodes[1])
    Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::GravityLine, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    nx, ny = Int.(nodes[1])

    Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], (1.0, 1.0, 1.0, 1.0))
    Ahorn.drawRectangle(ctx, x-4, y-4, 8, 8, (1.0, 1.0, 1.0, 0.4), (1.0, 1.0, 1.0, 0.4))
    Ahorn.drawRectangle(ctx, nx-4, ny-4, 8, 8, (1.0, 1.0, 1.0, 0.4), (1.0, 1.0, 1.0, 0.4))
end

end