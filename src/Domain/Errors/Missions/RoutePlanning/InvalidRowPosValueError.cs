namespace Domain.Errors.Missions.RoutePlanning;

public class InvalidRowPosValueError : DomainError
{
    public InvalidRowPosValueError(int rowPos, int rowDim) 
    : base("Invalid row position with respect to row dimension", "RgvMap.InvalidRowPosValue", $"A row position of {rowPos} is not within 0-{rowDim}")
    {
    }
}
