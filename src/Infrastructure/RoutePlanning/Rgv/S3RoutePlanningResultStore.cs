using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.RoutePlanning.Rgv;

public class S3RoutePlanningResultStore(IAmazonS3 s3Client, IOptions<RoutePlanningSettings> routePlanningSettings) : IRoutePlanningResultStore
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly string _bucketName = routePlanningSettings.Value.S3.BucketName;
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
        string key = $"{fileName}/{fileName}.png";

        UploadAsync(key, imageBytes, "image/png").GetAwaiter().GetResult();

        return key;
    }

    public void SaveRoutePlanningDetail(RoutePlanningDetailDto routePlanningDetail)
    {
        string stringJson = JsonSerializer.Serialize(routePlanningDetail, _jsonSerializerOptions);
        string key = $"{routePlanningDetail.Id}/{routePlanningDetail.Id}.json";

        UploadAsync(key, Encoding.UTF8.GetBytes(stringJson), "application/json").GetAwaiter().GetResult();
    }

    public RoutePlanningSummaryDto GetRoutePlanningSummary(string missionId)
    {
        string key = $"{missionId}/{missionId}.json";
        string jsonString = DownloadAsStringAsync(key).GetAwaiter().GetResult();

        var detail = JsonSerializer.Deserialize<RoutePlanningDetailDto>(jsonString)
            ?? throw new InvalidOperationException($"Route planning JSON for mission '{missionId}' deserialized to null");

        var rgvMapSummary = new RgvMapSummaryDto(detail.RgvMap.RowDim, detail.RgvMap.ColDim, detail.RgvMap.WidthLength, detail.RgvMap.HeightLength);
        var presignedImageUrls = detail.ImageUrls.Select(GetPresignedUrl).ToList();

        return new RoutePlanningSummaryDto(detail.Algorithm, presignedImageUrls, rgvMapSummary, detail.Score);
    }

    private string GetPresignedUrl(string key)
    {
        return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(1)
        });
    }

    private async Task UploadAsync(string key, byte[] content, string contentType)
    {
        using var stream = new MemoryStream(content);
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        });
    }

    private async Task<string> DownloadAsStringAsync(string key)
    {
        using var response = await _s3Client.GetObjectAsync(_bucketName, key);
        using var reader = new StreamReader(response.ResponseStream);
        return await reader.ReadToEndAsync();
    }
}
