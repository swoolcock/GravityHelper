module GravityHelperUpsideDownWatchTower

    using ..Ahorn, Maple

    @mapdef Entity "GravityHelper/UpsideDownWatchTower" UpsideDownWatchTower(
        x::Integer, y::Integer,
        summit::Bool=false, onlyY::Bool=false
    )

    const placements = Ahorn.PlacementDict(
        "Upside-Down Watchtower (GravityHelper)" => Ahorn.EntityPlacement(
            UpsideDownWatchTower
        ),
    )

    Ahorn.nodeLimits(entity::UpsideDownWatchTower) = 0, -1

    sprite = "objects/lookout/lookout05.png"

    function Ahorn.selection(entity::UpsideDownWatchTower)
        nodes = get(entity.data, "nodes", ())
        x, y = Ahorn.position(entity)

        res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y, sy=-1.0, jx=0.5, jy=1.0)]

        for node in nodes
            nx, ny = Int.(node)

            push!(res, Ahorn.getSpriteRectangle(sprite, nx, ny, sy=-1.0, jx=0.5, jy=1.0))
        end

        return res
    end

    function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::UpsideDownWatchTower)
        px, py = Ahorn.position(entity)

        for node in get(entity.data, "nodes", [])
            nx, ny = Int.(node)

            Ahorn.drawArrow(ctx, px, py + 8, nx, ny + 8, Ahorn.colors.selection_selected_fc, headLength=6)
            Ahorn.drawSprite(ctx, sprite, nx, ny, sy=-1.0, jx=0.5, jy=1.0)

            px, py = nx, ny
        end
    end

    function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::UpsideDownWatchTower, room::Maple.Room)
        x, y = Ahorn.position(entity)

        Ahorn.drawSprite(ctx, sprite, x, y, sy=-1.0, jx=0.5, jy=1.0)
    end

end