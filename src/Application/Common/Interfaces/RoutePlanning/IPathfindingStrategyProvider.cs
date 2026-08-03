using Domain.Missions.ValueObjects;
using FluentResults;

namespace Application.Common.Interfaces.RoutePlanning;

public interface IPathfindingStrategyProvider
{
    IReadOnlyCollection<RoutePlanningAlgorithm> SupportedAlgorithms { get; }

    Result<IPathfindingStrategy> GetStrategy(RoutePlanningAlgorithm algorithm);
}
