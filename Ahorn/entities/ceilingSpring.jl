module GravityHelperCeilingSpring

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/CeilingSpring" CeilingSpring(x::Integer, y::Integer, playerCanUse::Bool=true)

const placements = Ahorn.PlacementDict(
    "Spring (Down) (GravityHelper)" => Ahorn.EntityPlacement(
        CeilingSpring
    ),
)

function Ahorn.selection(entity::CeilingSpring)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 6, y - 2, 12, 5)
end

sprite = "objects/spring/00.png"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CeilingSpring, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 12, -2, rot=pi)

end