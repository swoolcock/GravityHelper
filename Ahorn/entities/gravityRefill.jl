module GravityHelperGravityRefill
    using ..Ahorn, Maple

    @mapdef Entity "GravityHelper/GravityRefill" GravityRefill(
        x::Integer, y::Integer,
        charges::Integer=1,
        oneUse::Bool=false,
        refillsDash::Bool=true,
        refillsStamina::Bool=true,
        respawnTime::Float64=2.5
    )

    const placements = Ahorn.PlacementDict(
        "Gravity Refill (GravityHelper)" => Ahorn.EntityPlacement(
            GravityRefill
        ),
    )

    const sprite = "objects/refill/idle00"

    function Ahorn.selection(entity::GravityRefill)
        x, y = Ahorn.position(entity)
        return Ahorn.getSpriteRectangle(sprite, x, y)
    end

    function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityRefill, room::Maple.Room)
        Ahorn.drawSprite(ctx, sprite, 0, 0)
    end
end