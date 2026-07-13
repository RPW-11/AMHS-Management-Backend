using System.Text.Json;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.RoutePlanning.Rgv;

public class LocalRoutePlanningResultStore(IOptions<RoutePlanningSettings> routePlanningSetting) : IRoutePlanningResultStore
{
    private readonly string _localRoutePlanningDirectory = routePlanningSetting.Value.Local.LocalDirectory;
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

    public string GetResultImageUrl(string missionId)
    {
        throw new NotImplementedException("Downloading via a URL is not supported by the local route planning result store");
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

        var detail = JsonSerializer.Deserialize<RoutePlanningDetailDto>(jsonString)
            ?? throw new InvalidOperationException($"Route planning JSON for mission '{missionId}' deserialized to null");

        var rgvMapSummary = new RgvMapSummaryDto(detail.RgvMap.RowDim, detail.RgvMap.ColDim, detail.RgvMap.WidthLength, detail.RgvMap.HeightLength);

        return new RoutePlanningSummaryDto(detail.Algorithm, detail.ImageUrls, rgvMapSummary, detail.Score);
    }
}
