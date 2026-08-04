using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Microsoft.Extensions.Options;

namespace Infrastructure.RoutePlanning.Storage;

public class S3RoutePlanningResultStore(IAmazonS3 s3Client, IOptions<RoutePlanningSettings> routePlanningSettings) : IRoutePlanningResultStore
{
    private const int PresignedUrlExpirationHours = 1;
    private const string SummaryObjectName = "summary.json";

    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly string _bucketName = routePlanningSettings.Value.S3.BucketName;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = false, PropertyNameCaseInsensitive = true };

    public async Task<string> WriteImageAsync(byte[] imageBytes, string missionId, RouteImageKind kind, CancellationToken cancellationToken = default)
    {
        string key = $"{missionId}/{kind.ToFileStem(missionId)}.png";

        await UploadAsync(key, imageBytes, "image/png", cancellationToken);

        return key;
    }

    public string GetResultImageUrl(string missionId)
    {
        string stem = RouteImageKind.Solved.ToFileStem(missionId);

        return GetPresignedUrl($"{missionId}/{stem}.png", $"attachment; filename=\"{stem}.png\"");
    }

    public string GetResultJsonUrl(string missionId)
    {
        return GetPresignedUrl(DetailKey(missionId), $"attachment; filename=\"{missionId}.json\"");
    }

    public async Task SaveRoutePlanningDetailAsync(RoutePlanningDetailDto routePlanningDetail, CancellationToken cancellationToken = default)
    {
        string missionId = routePlanningDetail.Id;

        string detailJson = JsonSerializer.Serialize(routePlanningDetail, _jsonSerializerOptions);
        await UploadAsync(DetailKey(missionId), Encoding.UTF8.GetBytes(detailJson), "application/json", cancellationToken);

        string summaryJson = JsonSerializer.Serialize(ToStoredSummary(routePlanningDetail), _jsonSerializerOptions);
        await UploadAsync(SummaryKey(missionId), Encoding.UTF8.GetBytes(summaryJson), "application/json", cancellationToken);
    }

    public async Task<RoutePlanningSummaryDto> GetRoutePlanningSummaryAsync(string missionId, CancellationToken cancellationToken = default)
    {
        RoutePlanningSummaryDto stored =
            await TryReadStoredSummaryAsync(missionId, cancellationToken)
            ?? await ReadSummaryFromDetailAsync(missionId, cancellationToken);

        var presignedImageUrls = stored.ImageUrls.Select(key => GetPresignedUrl(key)).ToList();

        return stored with { ImageUrls = presignedImageUrls };
    }

    private async Task<RoutePlanningSummaryDto?> TryReadStoredSummaryAsync(string missionId, CancellationToken cancellationToken)
    {
        try
        {
            string json = await DownloadAsStringAsync(SummaryKey(missionId), cancellationToken);

            return JsonSerializer.Deserialize<RoutePlanningSummaryDto>(json, _jsonSerializerOptions)
                ?? throw new InvalidOperationException($"Route planning summary for mission '{missionId}' deserialized to null");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<RoutePlanningSummaryDto> ReadSummaryFromDetailAsync(string missionId, CancellationToken cancellationToken)
    {
        string json = await DownloadAsStringAsync(DetailKey(missionId), cancellationToken);

        var detail = JsonSerializer.Deserialize<RoutePlanningDetailDto>(json, _jsonSerializerOptions)
            ?? throw new InvalidOperationException($"Route planning JSON for mission '{missionId}' deserialized to null");

        return ToStoredSummary(detail);
    }

    private static RoutePlanningSummaryDto ToStoredSummary(RoutePlanningDetailDto detail) =>
        new(detail.Algorithm,
            [.. detail.ImageUrls],
            new RgvMapSummaryDto(detail.RgvMap.RowDim, detail.RgvMap.ColDim, detail.RgvMap.WidthLength, detail.RgvMap.HeightLength),
            detail.Score);

    private static string DetailKey(string missionId) => $"{missionId}/{missionId}.json";

    private static string SummaryKey(string missionId) => $"{missionId}/{SummaryObjectName}";

    private string GetPresignedUrl(string key, string? contentDisposition = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(PresignedUrlExpirationHours)
        };

        if (contentDisposition is not null)
        {
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = contentDisposition
            };
        }

        return _s3Client.GetPreSignedURL(request);
    }

    private async Task UploadAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content);
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        }, cancellationToken);
    }

    private async Task<string> DownloadAsStringAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await _s3Client.GetObjectAsync(_bucketName, key, cancellationToken);
        using var reader = new StreamReader(response.ResponseStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

}
