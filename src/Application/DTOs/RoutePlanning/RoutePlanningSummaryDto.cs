using System.Text.Json.Serialization;

namespace Application.DTOs.RoutePlanning;

/// <summary>
/// The handful of fields a mission's detail view needs, stored separately so serving it does not
/// mean transferring the whole result document.
/// </summary>
/// <param name="ImageUrls">One solved image per routing, index-aligned with <paramref name="Scores"/>.</param>
/// <param name="Scores">One score per routing, best first.</param>
public record RoutePlanningSummaryDto(
    string Algorithm,
    IEnumerable<string> ImageUrls,
    RgvMapSummaryDto RgvMap,
    IEnumerable<RoutePlanningScoreDto> Scores
)
{
    /// <summary>
    /// Written by versions that stored a single routing. Read through <see cref="ReadScores"/>.
    /// </summary>
    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RoutePlanningScoreDto? LegacyScore { get; init; }

    /// <summary>
    /// The scores this document holds, whichever shape it was written in.
    /// </summary>
    public IReadOnlyList<RoutePlanningScoreDto> ReadScores()
    {
        if (Scores is not null)
        {
            return [.. Scores];
        }

        return LegacyScore is not null ? [LegacyScore] : [];
    }
}
