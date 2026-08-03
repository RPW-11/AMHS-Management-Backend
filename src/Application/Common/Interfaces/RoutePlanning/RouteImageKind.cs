namespace Application.Common.Interfaces.RoutePlanning;

public enum RouteImageKind
{
    Input,
    Solved
}

public static class RouteImageKindExtensions
{
    public static string ToFileStem(this RouteImageKind kind, string missionId) => kind switch
    {
        RouteImageKind.Input => $"{missionId}-input",
        RouteImageKind.Solved => missionId,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown route image kind")
    };
}
