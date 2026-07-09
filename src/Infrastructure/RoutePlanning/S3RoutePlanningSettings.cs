namespace Infrastructure.RoutePlanning;

public class S3RoutePlanningSettings
{
    public string SecretAccessKey { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string EndPointUrl { get; set; } = string.Empty;
}
