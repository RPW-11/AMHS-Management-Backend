namespace Domain.Errors.Missions.RoutePlanning;

public class DuplicateStationInClusterError : DomainError
{
    public DuplicateStationInClusterError(string stationName)
    : base("Duplicate station in cluster", "Cluster.DuplicateStation", $"Station '{stationName}' appears more than once in the same cluster")
    {
    }
}
