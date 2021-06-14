module GravityHelperGravityField

using ..Ahorn, Maple

@mapdef Entity "GravityHelper/GravityField" GravityField(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    gravityType::Integer=0, attachToSolids::Bool=false,
    arrowType::Integer=-2, fieldType::Integer=-2
)

const gravityColors = Dict{Integer, Tuple{Real, Real, Real, Real}}(
    -1 => (1.0, 1.0, 1.0, 0.5),
    0 => (0.0, 0.0, 1.0, 0.5),
    1 => (1.0, 0.0, 0.0, 0.5),
    2 => (0.5, 0.0, 0.5, 0.5),
)

const gravityTypes = Dict{String, Integer}(
    "None" => -1,
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const visualTypes = Dict{String, Integer}(
    "Default" => -2,
    "None" => -1,
    "Normal" => 0,
    "Inverted" => 1,
    "Toggle" => 2,
)

const placements = Ahorn.PlacementDict(
    "Gravity Field (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
    ),
    "Gravity Field (Attached Indicator) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "attachToSolids" => true,
            "drawArrows" => true,
            "drawField" => false,
            "visualOnly" => true
        )
    ),
    "Gravity Field (Visual Only) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "attachToSolids" => false,
            "drawArrows" => true,
            "drawField" => true,
            "visualOnly" => true
        )
    )
)

Ahorn.editingOptions(trigger::GravityField) = Dict{String, Any}(
    "gravityType" => gravityTypes,
    "arrowType" => visualTypes,
    "fieldType" => visualTypes
)

Ahorn.minimumSize(entity::GravityField) = 8, 8
Ahorn.resizable(entity::GravityField) = true, true

function Ahorn.selection(entity::GravityField)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GravityField, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    gravityType = Int(get(entity.data, "gravityType", 0))
    color = gravityColors[gravityType]
    
    Ahorn.Cairo.save(ctx)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)
    Ahorn.drawRectangle(ctx, 0, 0, width, height, color, (1.0, 1.0, 1.0, 1.0))
    Ahorn.restore(ctx)
end

end