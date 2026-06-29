namespace Application.Validation;

// Shared constants for the spatial/geometry validators (rooms, containers, doors).
internal static class GeometryRules
{
    // Hex colour: "#RRGGBB" or "#RRGGBBAA". Applied only when a colour is supplied (colours are optional).
    public const string HexColorPattern = "^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$";

    // Rotation is expressed in degrees on [0, 360).
    public const decimal MaxRotationExclusive = 360m;
}
