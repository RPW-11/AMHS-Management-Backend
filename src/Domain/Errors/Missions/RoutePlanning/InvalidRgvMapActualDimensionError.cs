namespace Domain.Errors.Missions.RoutePlanning;

public sealed class InvalidRgvMapActualDimensionError : DomainError
{
    public InvalidRgvMapActualDimensionError() : base("Invalid actual map dimension", "RgvMap.InvalidActualMapDimension", "Width and height dimension cannot be less than or equal to 0")
    {
    }
}
