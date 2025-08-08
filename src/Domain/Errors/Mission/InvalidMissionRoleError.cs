namespace Domain.Errors.Mission;

public class InvalidMissionRoleError : DomainError
{
    public InvalidMissionRoleError(string missionRole)
        : base("Invalid assigned mission role", "Mission.InvalidMissionRole", $"The role {missionRole} is not valid")
    {
    }
}
