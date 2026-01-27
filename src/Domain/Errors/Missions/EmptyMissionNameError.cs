namespace Domain.Errors.Missions;

public class EmptyMissionNameError : DomainError
{
    public EmptyMissionNameError() 
        : base("Mission name cannot be empty", "Mission.EmptyMissionName")
    {
    }
}
