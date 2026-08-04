using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.BackgroundJobHub;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.RoutePlanning;
using Domain.Missions;
using Domain.Missions.ValueObjects;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.RoutePlanningService;

public class RoutePlanningService : BaseService, IRoutePlanningService
{
    private readonly IPathfindingStrategyProvider _strategyProvider;
    private readonly ISourceImageValidator _sourceImageValidator;
    private readonly IBackgroundJobHub _backgroundJobHub;
    private readonly IMissionRepository _missionRepository;
    private readonly IDomainDispatcher _domainDispatcher;
    private readonly ILogger<RoutePlanningService> _logger;

    public RoutePlanningService(IPathfindingStrategyProvider strategyProvider,
                                ISourceImageValidator sourceImageValidator,
                                IBackgroundJobHub backgroundJobHub,
                                IMissionRepository missionRepository,
                                IDomainDispatcher domainDispatcher,
                                IUnitOfWork unitOfWork,
                                ILogger<RoutePlanningService> logger)
                                : base(unitOfWork)
    {
        _strategyProvider = strategyProvider;
        _sourceImageValidator = sourceImageValidator;
        _backgroundJobHub = backgroundJobHub;
        _missionRepository = missionRepository;
        _domainDispatcher = domainDispatcher;
        _logger = logger;
    }

    public async Task<Result> EnqueueRoutePlanning(RoutePlanningRequest request)
    {
        var (missionId, imageBytes, imageContentType, algorithm, rowDim, colDim, widthLength, heightLength, points, clusters, clusterFlows) = request;

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId,
            ["Algorithm"] = algorithm,
            ["GridSize"] = $"{rowDim}×{colDim}",
            ["ActualDimension"] = $"{widthLength}x{heightLength}"
        });

        _logger.LogInformation("Route planning request started | Input points: {PointCount}", points.Count());

        var missionIdResult = RequireValid(MissionId.FromString(missionId), "Invalid mission ID format");
        if (missionIdResult.IsFailed)
        {
            return Result.Fail(missionIdResult.Errors);
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            _logger.LogWarning("Mission not found");
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        if (missionResult.Value.Category != MissionCategory.RoutePlanning)
        {
            _logger.LogWarning("Mission category mismatch - expected RoutePlanning, got {Category}",
                missionResult.Value.Category);
            return Result.Fail(ApplicationError.Validation("The selected mission is not a route-planning mission"));
        }

        if (missionResult.Value.Status == MissionStatus.Processing)
        {
            _logger.LogWarning("Route planning is already in progress for this mission");
            return Result.Fail(ApplicationError.Duplicated("Route planning is already in progress for this mission"));
        }

        _logger.LogDebug("Mission validated | Category: {Category} | Name: {Name}",
            missionResult.Value.Category, missionResult.Value.Name ?? "(no name)");

        var imageResult = _sourceImageValidator.Validate(imageBytes, imageContentType);
        if (imageResult.IsFailed)
        {
            _logger.LogWarning("Rejected source layout image: {ErrorMessage}", imageResult.Errors[0].Message);
            return Result.Fail(imageResult.Errors);
        }

        var algorithmResult = RequireValid(RoutePlanningAlgorithm.FromString(algorithm), $"Unsupported or invalid algorithm '{algorithm}'");
        if (algorithmResult.IsFailed)
        {
            return Result.Fail(algorithmResult.Errors);
        }

        // Algorithm check
        var strategyResult = _strategyProvider.GetStrategy(algorithmResult.Value);
        if (strategyResult.IsFailed)
        {
            _logger.LogWarning("Algorithm '{Algorithm}' has no registered strategy", algorithm);
            return Result.Fail(strategyResult.Errors);
        }

        var rgvMapResult = new RgvMapBuilder()
            .WithGrid(rowDim, colDim, widthLength, heightLength)
            .WithPoints(points)
            .WithClusters(clusters)
            .WithClusterFlows(clusterFlows)
            .Build();

        if (rgvMapResult.IsFailed)
        {
            _logger.LogWarning("Failed to build the RGV map: {ErrorMessage}", rgvMapResult.Errors[0].Message);
            return Result.Fail(rgvMapResult.Errors);
        }

        RgvMap rgvMap = rgvMapResult.Value;

        _logger.LogDebug("RGV map created with {FlowCount} cluster flows | Grid size: {RowDim}x{ColDim}",
                rgvMap.ClusterFlows.Count, rowDim, colDim);

        MissionBase mission = missionResult.Value;
        MissionId parsedMissionId = missionIdResult.Value;

        mission.ProcessRoutePlanning();

        var updateResult = _missionRepository.UpdateMission(mission);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed before enqueueing route planning");
            return Result.Fail(ApplicationError.Internal);
        }

        var job = new RoutePlanningJob(parsedMissionId, rgvMap, algorithmResult.Value, imageBytes);

        bool enqueued = _backgroundJobHub.TryEnqueue(
            (sp, ct) => sp.GetRequiredService<IRoutePlanningJobHandler>().HandleAsync(job, ct),
            out _);

        if (!enqueued)
        {
            _logger.LogWarning("Route planning queue is full, mission {MissionId} was not enqueued", parsedMissionId);
            await RevertProcessingStatus(mission);
            return Result.Fail(ApplicationError.Validation("The route planning queue is full, please try again later"));
        }

        await _domainDispatcher.DispatchAsync(mission.DomainEvents);
        mission.ClearDomainEvents();

        _logger.LogInformation("Route planning is being processed | Mission status updated to Processing");

        return Result.Ok();
    }

    private async Task RevertProcessingStatus(MissionBase mission)
    {
        mission.SetMissionStatus(MissionStatus.Failed);

        mission.ClearDomainEvents();

        var revertResult = _missionRepository.UpdateMission(mission);
        if (revertResult.IsFailed)
        {
            _logger.LogError("Failed to revert mission status after a rejected enqueue: {ErrorMessage}", revertResult.Errors[0].Message);
            return;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit the reverted mission status after a rejected enqueue");
        }
    }

    private Result<T> RequireValid<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            var error = result.Errors[0];
            _logger.LogWarning("{Context}: {ErrorMessage}", context, error.Message);

            string detail = error.Metadata.TryGetValue("detail", out var value) ? value?.ToString() ?? "" : "";

            return Result.Fail<T>(new ApplicationError(error.Message, "Validation", detail));
        }

        return result;
    }
}
