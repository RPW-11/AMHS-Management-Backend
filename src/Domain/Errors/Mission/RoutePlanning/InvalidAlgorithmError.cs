namespace Domain.Errors.Mission.RoutePlanning;

public class InvalidAlgorithmError : DomainError
{
    public InvalidAlgorithmError(string algorithm) 
    : base("Invalid algorithm input", "Mission.InvalidRoutePlanningAlgorithm", $"The algorithm '{algorithm}' is invalid")
    {
    }
}
