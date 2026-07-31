namespace Domain.Errors.Missions.RoutePlanning;

public class DuplicateClusterNameError : DomainError
{
    public DuplicateClusterNameError(string clusterName)
    : base("Duplicate cluster name", "RgvMap.DuplicateClusterName", $"Two different clusters are both named '{clusterName}'")
    {
    }
}
