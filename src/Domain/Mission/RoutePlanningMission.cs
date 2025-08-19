using Domain.Mission.ValueObjects;

namespace Domain.Mission;

public sealed class RoutePlanningMission : MissionBase
{
    public string ImageUrl { get; private set; }
    public RoutePlanningAlgorithm Algorithm { get; private set; }
    public RgvMap RgvMap { get; private set; }

    private RoutePlanningMission(MissionBase missionBase,
                                 string imageUrl,
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
        ImageUrl = imageUrl;
        RgvMap = rgvMap;
    }

    public static RoutePlanningMission FromBaseClass(MissionBase missionBase, string imageUrl, RoutePlanningAlgorithm algorithm, RgvMap rgvMap)
    {
        return new(
            missionBase,
            imageUrl,
            algorithm,
            rgvMap
        );
    }

    public void SetRgvMap(RgvMap newMap)
    {
        RgvMap = newMap;
    }

    public void SetImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void SetAlgorithm(RoutePlanningAlgorithm algorithm)
    {
        Algorithm = algorithm;
    }
}
