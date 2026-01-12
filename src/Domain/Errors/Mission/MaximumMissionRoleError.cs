namespace Domain.Errors.Mission;

public class MaximumMissionRoleError : DomainError
{
    public MaximumMissionRoleError() 
        : base("There must be 1 mission leader and 2 co-leaders", "Mission.MaximumMissionRole")
    {
    }
}
