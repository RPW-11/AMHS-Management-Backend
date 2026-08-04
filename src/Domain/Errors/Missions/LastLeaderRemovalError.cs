namespace Domain.Errors.Missions;

public class LastLeaderRemovalError : DomainError
{
    public LastLeaderRemovalError()
    : base("Cannot remove the mission's only leader",
           "Mission.LastLeaderRemoval",
           "A mission must always have a leader. Promote another member to leader before removing this one")
    {
    }
}
