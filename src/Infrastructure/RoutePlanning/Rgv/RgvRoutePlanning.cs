using System.Text.Json;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Microsoft.Extensions.Options;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning(IOptions<RoutePlanningSettings> routePlanningSetting) : IRgvRoutePlanning
{
    private const int MaxGenerationsNumber = 400;

    private readonly string _localRoutePlanningDirectory = routePlanningSetting.Value.LocalDirectory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public byte[] DrawMultipleFlows(
        byte[] imageBytes,
        Grid grid,
        List<(List<PathPoint> Solution, string ArrowColor)> routes)
    {
        if (routes.Count == 0)
            throw new ArgumentException("No route details provided");

        using var drawer = new RouteDrawer(imageBytes, grid);
        foreach (var (solution, arrowColor) in routes)
        {
            drawer.DrawSolution(solution, arrowColor);
        }
        drawer.DrawStations(GetStations(grid));

        return drawer.Encode();
    }

    private static IEnumerable<Station> GetStations(Grid grid)
    {
        for (int row = 0; row < grid.RowDim; row++)
        {
            for (int col = 0; col < grid.ColDim; col++)
            {
                if (grid.MapMatrix[row, col] is Station station)
                {
                    yield return station;
                }
            }
        }
    }

    public string WriteImage(byte[] imageBytes, string fileName)
    {
        string outputPath = System.IO.Path.Combine(_localRoutePlanningDirectory, fileName + ".png");

        Directory.CreateDirectory(_localRoutePlanningDirectory);
        File.WriteAllBytes(outputPath, imageBytes);

        return outputPath;
    }

    public IEnumerable<PathPoint> Solve(
        Grid grid,
        List<PathPoint> stationsOrder,
        List<List<PathPoint>> currentRoutePoints,
        RoutePlanningAlgorithm routePlanningAlgorithm,
        int generationsNumber
    )
    {
        if (generationsNumber <= 0 || generationsNumber > MaxGenerationsNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generationsNumber),
                generationsNumber,
                $"Generations number must be between 1 and {MaxGenerationsNumber}");
        }

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.ReinforcementLearning)
        {
            throw new NotImplementedException("Reinforcement learning route planning is not implemented yet");
        }

        var gaSolver = new GeneticAlgorithmSolver(grid, stationsOrder, currentRoutePoints, generationsNumber);
        return gaSolver.Solve();
    }

    public void SaveRoutePlanningDetail(RoutePlanningDetailDto routePlanningDetail)
    {
        string stringJson = JsonSerializer.Serialize(routePlanningDetail, _jsonSerializerOptions);
        string path = System.IO.Path.Combine(_localRoutePlanningDirectory, routePlanningDetail.Id) + ".json";
        File.WriteAllText(path, stringJson);
    }

    public RoutePlanningSummaryDto GetRoutePlanningSummary(string missionId)
    {
        string path = System.IO.Path.Combine(_localRoutePlanningDirectory, missionId) + ".json";
        string jsonString = File.ReadAllText(path);

        return JsonSerializer.Deserialize<RoutePlanningSummaryDto>(jsonString)
            ?? throw new InvalidOperationException($"Route planning JSON for mission '{missionId}' deserialized to null");
    }

    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder)
    {
        var (throughput, trackLength, numOfRgvs, optimality) = RouteEvaluator.GetSolutionScores(solution, grid, stationsOrder);

        return new(throughput, trackLength, numOfRgvs, optimality);
    }
}
