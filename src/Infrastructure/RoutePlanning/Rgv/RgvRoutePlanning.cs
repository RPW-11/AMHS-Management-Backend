using System.Text.Json;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Microsoft.Extensions.Options;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning(IOptions<RoutePlanningSettings> routePlanningSetting) : IRgvRoutePlanning
{
    private readonly string _localRoutePlanningDirectory = routePlanningSetting.Value.LocalDirectory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public byte[] DrawMultipleFlows(
        byte[] imageBytes,
        List<RgvMap> mapsWithSolutions,
        List<PathPoint> intersections)
    {
        if (mapsWithSolutions.Count == 0)
            throw new ArgumentException("No route details provided");

        return RouteDrawer.DrawMultipleRoutes(
            imageBytes,
            mapsWithSolutions,
            intersections
        );
    }

    public string WriteImage(byte[] imageBytes, string fileName)
    {
        string outputPath = Path.Combine(_localRoutePlanningDirectory, fileName + ".png");

        Directory.CreateDirectory(_localRoutePlanningDirectory);
        File.WriteAllBytes(outputPath, imageBytes);

        return outputPath;
    }

    public (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(
        RgvMap rgvMap,
        List<List<PathPoint>> currentRoutePoints,
        RoutePlanningAlgorithm routePlanningAlgorithm
    )
    {

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.Dfs)
        {
            var result = DfsSolver.FindBestRoute(rgvMap);
            var preprocessedResult = PostProcessingRoute.SmoothAndRasterizeFourDirections(result, rgvMap);

            return (result, preprocessedResult);
        }
        if (routePlanningAlgorithm == RoutePlanningAlgorithm.GeneticAlgorithm)
        {
            var gaSolver = new GeneticAlgorithmSolver(rgvMap, currentRoutePoints);
            var result = gaSolver.Solve();
            var preprocessedResult = PostProcessingRoute.SmoothAndRasterizeFourDirections(result, rgvMap);

            return (result, preprocessedResult);
        }

        throw new Exception($"Algorithm '{routePlanningAlgorithm}' is not implemented");
    }

    public string WriteToJson(RoutePlanningDetailDto routePlanningDetailDto)
    {
        try
        {
            string stringJson = JsonSerializer.Serialize(routePlanningDetailDto, _jsonSerializerOptions);
            string path = Path.Combine(_localRoutePlanningDirectory, routePlanningDetailDto.Id) + ".json";
            File.WriteAllText(path, stringJson);
            return path;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw new Exception (error.Message);
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

    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap)
    {
        var (throughput, trackLength, numOfRgvs) = RouteEvaluator.GetSolutionScores(solution, rgvMap);

        return new(throughput, trackLength, numOfRgvs);
    }
}
