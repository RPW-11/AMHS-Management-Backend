using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RouteResultPersister(
    IRoutePlanningResultStore routePlanningResultStore,
    IRouteImageRenderer routeImageRenderer,
    ILogger<RouteResultPersister> logger) : IRouteResultPersister
{
    private readonly IRoutePlanningResultStore _routePlanningResultStore = routePlanningResultStore;
    private readonly IRouteImageRenderer _routeImageRenderer = routeImageRenderer;
    private readonly ILogger<RouteResultPersister> _logger = logger;

    public async Task PersistAsync(
        MissionBase mission,
        Grid grid,
        RoutePlanningAlgorithm algorithm,
        byte[] imageBytes,
        IReadOnlyList<ComposedSolution> solutions,
        RgvMapDetailDto rgvMap,
        IEnumerable<ClusterDefinitionDto> clusters,
        IEnumerable<ClusterFlowDefinitionDto> clusterFlows,
        CancellationToken cancellationToken = default)
    {
        string missionId = mission.Id.ToString();

        var inputImagePath = await _routePlanningResultStore.WriteImageAsync(
            imageBytes, missionId, RouteImageKind.Input, cancellationToken: cancellationToken);

        List<RoutePlanningSolutionDto> solutionDtos = [];

        for (int i = 0; i < solutions.Count; i++)
        {
            ComposedSolution solution = solutions[i];

            // 1-based: the first routing keeps the unsuffixed image key earlier solves used.
            int solutionNumber = i + 1;

            var drawnImageBytes = _routeImageRenderer.Render(imageBytes, grid, solution.Routes);

            var imagePath = await _routePlanningResultStore.WriteImageAsync(
                drawnImageBytes, missionId, RouteImageKind.Solved, solutionNumber, cancellationToken);

            solutionDtos.Add(new RoutePlanningSolutionDto(
                imagePath,
                RoutePlanningDtoMapper.ToClusterFlowSolutionDtos(solution.ClusterFlows),
                solution.Score));
        }

        var routePlanningDetail = new RoutePlanningDetailDto(
            missionId,
            algorithm.ToString(),
            inputImagePath,
            rgvMap,
            clusters,
            clusterFlows,
            solutionDtos);

        await _routePlanningResultStore.SaveRoutePlanningDetailAsync(routePlanningDetail, cancellationToken);
        _logger.LogInformation("Route planning data saved for mission {MissionId} with {SolutionCount} routings",
            mission.Id, solutionDtos.Count);

        mission.Finish();
    }
}
