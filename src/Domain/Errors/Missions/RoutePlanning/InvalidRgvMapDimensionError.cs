namespace Domain.Errors.Missions.RoutePlanning;

public sealed class InvalidRgvMapDimensionError : DomainError
{
    public InvalidRgvMapDimensionError() : base("Invalid map dimension", "RgvMap.InvalidMapDimension", "Row and col dimension must be at least a value of 3")
    {
    }
}
