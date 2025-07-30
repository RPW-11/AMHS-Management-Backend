namespace Domain.Errors.Mission.RoutePlanning;

public class InvalidNumberOfStationsOrderError : DomainError
{
    public InvalidNumberOfStationsOrderError()
    : base("Invalid number of stations order", "RgvMap.InvalidNumberOfStationsOrder", "The number of stations order must be at least 2")
    {
    }
}
