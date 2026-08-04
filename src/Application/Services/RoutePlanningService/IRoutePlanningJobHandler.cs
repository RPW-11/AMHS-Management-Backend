namespace Application.Services.RoutePlanningService;

public interface IRoutePlanningJobHandler
{
    Task HandleAsync(RoutePlanningJob job, CancellationToken cancellationToken = default);
}
