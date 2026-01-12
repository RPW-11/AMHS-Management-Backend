namespace Domain.Errors.Mission;

public class InvalidMissionStatusError : DomainError
{
    public InvalidMissionStatusError(string status) 
    : base("Invalid mission status", "Mission.InvalidMissionStatus", $"the mission status '{status}' is invalid")
    {
    }
}
