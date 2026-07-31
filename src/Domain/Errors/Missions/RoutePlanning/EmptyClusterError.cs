namespace Domain.Errors.Missions.RoutePlanning;

public class EmptyClusterError : DomainError
{
    public EmptyClusterError(string clusterName)
    : base("Empty cluster", "Cluster.Empty", $"Cluster '{clusterName}' must contain at least one station")
    {
    }
}
