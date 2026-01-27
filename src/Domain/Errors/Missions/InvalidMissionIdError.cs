namespace Domain.Errors.Missions;

public class InvalidMissionIdError : DomainError
{
    public InvalidMissionIdError(string id) 
        : base("Invalid mission id", "Mission.InvalidMissionId", $"The {id} mission id is invalid.")
    {
    }
}
