using System.Text.Json.Serialization;

namespace Application.DTOs.RoutePlanning;

/// <summary>
/// The full stored result of a solve. The grid, clusters and flow definitions describe the layout
/// and so are held once; only <see cref="Solutions"/> varies between routings.
/// </summary>
public record RoutePlanningDetailDto(
    string Id,
    string Algorithm,
    string InputImageUrl,
    RgvMapDetailDto RgvMap,
    IEnumerable<ClusterDefinitionDto> Clusters,
    IEnumerable<ClusterFlowDefinitionDto> ClusterFlows,
    IEnumerable<RoutePlanningSolutionDto> Solutions
)
{
    /// <summary>
    /// Written by versions that stored a single routing, and absent from anything written since.
    /// Read through <see cref="ReadSolutions"/> rather than touching these directly.
    /// </summary>
    [JsonPropertyName("routes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ClusterFlowSolutionDto>? LegacyRoutes { get; init; }

    /// <inheritdoc cref="LegacyRoutes"/>
    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RoutePlanningScoreDto? LegacyScore { get; init; }

    /// <inheritdoc cref="LegacyRoutes"/>
    [JsonPropertyName("imageUrls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? LegacyImageUrls { get; init; }

    /// <summary>
    /// The routings this document holds, whichever shape it was written in. A document predating
    /// multiple routings yields the single routing it stored, so missions solved before this
    /// keep reading without a backfill.
    /// </summary>
    public IReadOnlyList<RoutePlanningSolutionDto> ReadSolutions()
    {
        if (Solutions is not null)
        {
            return [.. Solutions];
        }

        if (LegacyRoutes is null || LegacyScore is null)
        {
            return [];
        }

        return [new RoutePlanningSolutionDto(LegacyImageUrls?.FirstOrDefault() ?? "", LegacyRoutes, LegacyScore)];
    }
}
