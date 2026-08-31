namespace Application.Common.Interfaces.RoutePlanning;

public enum RouteImageKind
{
    Input,
    Solved
}

public static class RouteImageKindExtensions
{
    /// <summary>
    /// The file name, without extension, an image of this kind is stored under.
    /// </summary>
    /// <param name="solutionNumber">
    /// Which routing the image draws, 1-based. The first keeps the unsuffixed name every solve
    /// used before there were several, so results stored back then still resolve.
    /// </param>
    public static string ToFileStem(this RouteImageKind kind, string missionId, int solutionNumber = 1) => kind switch
    {
        RouteImageKind.Input => $"{missionId}-input",
        RouteImageKind.Solved => solutionNumber <= 1 ? missionId : $"{missionId}-{solutionNumber}",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown route image kind")
    };
}
