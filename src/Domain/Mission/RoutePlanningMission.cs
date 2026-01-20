using Domain.Mission.ValueObjects;

namespace Domain.Mission;

public sealed class RoutePlanningMission : MissionBase
{
    public IEnumerable<string> ImageUrls { get; private set; }
    public RoutePlanningAlgorithm Algorithm { get; private set; }
    public RgvMap RgvMap { get; private set; }

    private RoutePlanningMission(MissionBase missionBase,
                                 IEnumerable<string> imageUrls,
                                 RoutePlanningAlgorithm algorithm,
                                 RgvMap rgvMap)
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
        RgvMap = rgvMap;
    }

    public static RoutePlanningMission FromBaseClass(MissionBase missionBase, RoutePlanningAlgorithm algorithm, RgvMap rgvMap)
    {
        return new(
            missionBase,
            [],
            algorithm,
            rgvMap
        );
    }

    public void SetRgvMap(RgvMap newMap)
    {
        RgvMap = newMap;
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
