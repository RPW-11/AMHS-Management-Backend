namespace Infrastructure.RoutePlanning;

public class RoutePlanningSettings
{
    public const string SectionName = "RoutePlanningSettings";
    public S3RoutePlanningSettings S3 { get; set; } = new();
}
