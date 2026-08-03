namespace Domain.Errors.Missions.RoutePlanning;

public class SharedStationBetweenAdjacentClustersError : DomainError
{
    public SharedStationBetweenAdjacentClustersError(string fromClusterName, string toClusterName, string stationName)
    : base("Shared station between adjacent clusters",
           "ClusterFlow.SharedStationBetweenAdjacentClusters",
           $"Clusters '{fromClusterName}' and '{toClusterName}' are adjacent in a flow but both contain station '{stationName}', leaving no connector to solve between them")
    {
    }
}
