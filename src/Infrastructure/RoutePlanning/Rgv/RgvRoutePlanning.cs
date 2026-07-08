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
        List<(List<PathPoint> Solution, string ArrowColor)> routes,
        List<PathPoint> intersections)
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

    public (IEnumerable<PathPoint> RawPath, IEnumerable<PathPoint> SmoothedPath) Solve(
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

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.GeneticAlgorithm)
        {
            var gaSolver = new GeneticAlgorithmSolver(grid, stationsOrder, currentRoutePoints, generationsNumber);
            var result = gaSolver.Solve();
            var preprocessedResult = PostProcessingRoute.SmoothAndRasterizeFourDirections(result, grid);

            return (result, preprocessedResult);
        }

        throw new Exception($"Algorithm '{routePlanningAlgorithm}' is not implemented");
    }

    public string WriteToJson(RoutePlanningDetailDto routePlanningDetailDto)
    {
        try
        {
            string stringJson = JsonSerializer.Serialize(routePlanningDetailDto, _jsonSerializerOptions);
            string path = System.IO.Path.Combine(_localRoutePlanningDirectory, routePlanningDetailDto.Id) + ".json";
            File.WriteAllText(path, stringJson);
            return path;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw new Exception(error.Message);
        }
    }

    public RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonFileUrl);
            RoutePlanningSummaryDto? routePlanningDetail = JsonSerializer.Deserialize<RoutePlanningSummaryDto>(jsonString) ?? throw new Exception("Error serializing the resource file");

            return routePlanningDetail;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw new Exception(error.Message);
        }
    }

    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, Grid grid, List<PathPoint> stationsOrder)
    {
        var (throughput, trackLength, numOfRgvs) = RouteEvaluator.GetSolutionScores(solution, grid, stationsOrder);

        return new(throughput, trackLength, numOfRgvs);
    }
}
