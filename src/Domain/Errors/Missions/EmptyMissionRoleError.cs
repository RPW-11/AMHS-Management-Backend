namespace Domain.Errors.Missions;

public class EmptyMissionRoleError : DomainError
{
    public EmptyMissionRoleError() 
        : base("Mission role cannot be empty", "Mission.EmptyMissionRole")
    {
    }
}
