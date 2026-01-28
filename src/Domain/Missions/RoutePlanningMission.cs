using Domain.Missions.ValueObjects;

namespace Domain.Missions;

public sealed class RoutePlanningMission : MissionBase
{
    public IEnumerable<string> ImageUrls { get; private set; }
    public RoutePlanningAlgorithm Algorithm { get; private set; }
    public IEnumerable<RgvMap> RgvMaps { get; private set; }

    private RoutePlanningMission(MissionBase missionBase,
                                 IEnumerable<string> imageUrls,
                                 RoutePlanningAlgorithm algorithm,
                                 IEnumerable<RgvMap> rgvMaps)
    : base(missionBase.Id,
          missionBase.Name,
          missionBase.Description,
          missionBase.Category,
          missionBase.Status,
          [.. missionBase.AssignedEmployees],
          missionBase.FinishedAt)
    {
        Algorithm = algorithm;
        ImageUrls = imageUrls;
        RgvMaps = rgvMaps;
    }

    public static RoutePlanningMission FromBaseClass(MissionBase missionBase, RoutePlanningAlgorithm algorithm, IEnumerable<RgvMap> rgvMaps)
    {
        return new(
            missionBase,
            [],
            algorithm,
            rgvMaps
        );
    }

    public void SetRgvMaps(IEnumerable<RgvMap> newMaps)
    {
        RgvMaps = newMaps;
    }

    public void AddImageUrl(string imageUrl)
    {
        ImageUrls = ImageUrls.Append(imageUrl);
    }

    public void SetAlgorithm(RoutePlanningAlgorithm algorithm)
    {
        Algorithm = algorithm;
    }
}
