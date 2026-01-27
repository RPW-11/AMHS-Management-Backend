namespace Domain.Errors.Missions.RoutePlanning;

public class InvalidColPosValueError : DomainError
{
    public InvalidColPosValueError(int colPos, int colDim) 
    : base("Invalid column position with respect to column dimension", "RgvMap.InvalidColPosValue", $"A column position of {colPos} is not within 0-{colDim}")
    {
    }
}
