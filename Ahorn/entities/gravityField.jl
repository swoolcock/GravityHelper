module GravityHelperGravityField

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"

@mapdef Entity "GravityHelper/GravityField" GravityField(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    gravityType::Integer=0, attachToSolids::Bool=false,
    arrowType::Integer=-2, fieldType::Integer=-2, sound::String="",
    arrowOpacity::Real=0.5, fieldOpacity::Real=0.15, particleOpacity::Real=0.5,
    arrowColor::String="FFFFFF", fieldColor::String="", particleColor::String="FFFFFF",
    affectsPlayer::Bool=true, affectsHoldableActors::Bool=false, affectsOtherActors::Bool=false,
    momentumMultiplier::Real=1.0
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
            "defaultToController" => false,
            "attachToSolids" => true,
            "arrowOpacity" => 1,
            "fieldOpacity" => 0,
            "particleOpacity" => 0,
            "fieldType" => -1,
            "affectsPlayer" => false,
        )
    ),
    "Gravity Field (Visual Only) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "affectsPlayer" => false,
        )
    )
)

Ahorn.editingIgnored(entity::GravityField, multiple::Bool=false) = String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityField) = Dict{String, Any}(
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
