namespace Domain.Errors.Missions.RoutePlanning;

public class AdjacentDuplicateClusterError : DomainError
{
    public AdjacentDuplicateClusterError(string clusterName)
    : base("Adjacent duplicate cluster in flow", "ClusterFlow.AdjacentDuplicateCluster", $"Cluster '{clusterName}' cannot follow itself in a cluster flow")
    {
    }
}
