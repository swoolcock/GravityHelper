module GravityHelperVvvvvvTrigger

using ..Ahorn, Maple

@mapdef Trigger "GravityHelper/VvvvvvTrigger" VvvvvvTrigger(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    enable::Bool=true,
    onlyOnSpawn::Bool=true
)

const placements = Ahorn.PlacementDict(
    "VVVVVV Trigger (GravityHelper)" => Ahorn.EntityPlacement(
        VvvvvvTrigger,
        "rectangle",
    ),
)

end