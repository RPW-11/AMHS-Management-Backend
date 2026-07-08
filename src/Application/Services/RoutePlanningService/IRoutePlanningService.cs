using Application.DTOs.RoutePlanning;
using FluentResults;

namespace Application.Services.RoutePlanningService;

public interface IRoutePlanningService
{
    Task<Result> EnqueueRoutePlanning(RoutePlanningRequest request);
}
