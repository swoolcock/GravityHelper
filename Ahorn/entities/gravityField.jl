# Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
# See the LICENCE file in the repository root for full licence text.

module GravityHelperGravityField

using ..Ahorn, Maple

const PLUGIN_VERSION = "1"
const DEFAULT_SINGLE_USE_SOUND = "event:/new_content/game/10_farewell/glider_emancipate"

@mapdef Entity "GravityHelper/GravityField" GravityField(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    pluginVersion::String=PLUGIN_VERSION,
    defaultToController::Bool=true,
    gravityType::Integer=0, exitGravityType::Integer=-1, attachToSolids::Bool=false,
    arrowType::Integer=-2, fieldType::Integer=-2, sound::String="",
    arrowOpacity::Real=0.5, fieldOpacity::Real=0.15, particleOpacity::Real=0.5,
    arrowColor::String="FFFFFF", fieldColor::String="", particleColor::String="FFFFFF",
    flashOnTrigger::Bool=true,
    affectsPlayer::Bool=true, affectsHoldableActors::Bool=false, affectsOtherActors::Bool=false,
    momentumMultiplier::Real=1.0, singleUse::Bool=false, singleUseSound::String=DEFAULT_SINGLE_USE_SOUND,
    cassetteIndex::Integer=-1, cassetteSequence::String=""
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

const exitGravityTypes = Dict{String, Integer}(
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
    "Gravity Field (Normal) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 0,
        )
    ),
    "Gravity Field (Inverted) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 1,
        )
    ),
    "Gravity Field (Toggle) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 2,
        )
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
    ),
    "Gravity Field (Cassette Block 0 - Blue) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 0,
            "cassetteIndex" => 0,
            "fieldColor" => "49aaf0",
        )
    ),
    "Gravity Field (Cassette Block 1 - Rose) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 1,
            "cassetteIndex" => 1,
            "fieldColor" => "f049be",
        )
    ),
    "Gravity Field (Cassette Block 2 - Bright Sun) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 0,
            "cassetteIndex" => 2,
            "fieldColor" => "fcdc3a",
        )
    ),
    "Gravity Field (Cassette Block 3 - Malachite) (GravityHelper)" => Ahorn.EntityPlacement(
        GravityField,
        "rectangle",
        Dict{String, Any}(
            "gravityType" => 1,
            "cassetteIndex" => 3,
            "fieldColor" => "38e04e",
        )
    )
)

Ahorn.editingIgnored(entity::GravityField, multiple::Bool=false) = multiple ? String["x", "y", "modVersion", "pluginVersion"] : String["modVersion", "pluginVersion"]

Ahorn.editingOptions(entity::GravityField) = Dict{String, Any}(
    "gravityType" => gravityTypes,
    "exitGravityType" => exitGravityTypes,
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
