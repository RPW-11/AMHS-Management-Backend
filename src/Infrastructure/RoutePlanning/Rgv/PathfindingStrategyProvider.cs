using Application.Common.Errors;
using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;
using FluentResults;

namespace Infrastructure.RoutePlanning.Rgv;

// Keys the registered strategies by algorithm, so adding one is a registration in
// DependencyInjection rather than a branch here.
public class PathfindingStrategyProvider : IPathfindingStrategyProvider
{
    private readonly Dictionary<RoutePlanningAlgorithm, IPathfindingStrategy> _strategiesByAlgorithm;

    public PathfindingStrategyProvider(IEnumerable<IPathfindingStrategy> strategies)
    {
        // Throws on a duplicate algorithm, failing at startup rather than silently shadowing one.
        _strategiesByAlgorithm = strategies.ToDictionary(strategy => strategy.Algorithm);
    }

    public IReadOnlyCollection<RoutePlanningAlgorithm> SupportedAlgorithms => _strategiesByAlgorithm.Keys;

    public Result<IPathfindingStrategy> GetStrategy(RoutePlanningAlgorithm algorithm)
    {
        if (_strategiesByAlgorithm.TryGetValue(algorithm, out var strategy))
        {
            return Result.Ok(strategy);
        }

        string supported = string.Join(", ", SupportedAlgorithms);
        return Result.Fail(ApplicationError.Validation(
            $"Route planning with '{algorithm}' is not supported, the available algorithms are {supported}"));
    }
}
