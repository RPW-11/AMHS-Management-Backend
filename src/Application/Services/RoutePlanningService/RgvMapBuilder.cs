using Application.Common.Errors;
using Application.DTOs.RoutePlanning;
using Domain.Missions.ValueObjects;
using FluentResults;

namespace Application.Services.RoutePlanningService;

public sealed class RgvMapBuilder
{
    private int _rowDim;
    private int _colDim;
    private int _widthLength;
    private int _heightLength;
    private IEnumerable<PathPointDto> _points = [];
    private IEnumerable<ClusterDto> _clusters = [];
    private IEnumerable<ClusterFlowDto> _clusterFlows = [];

    public RgvMapBuilder WithGrid(int rowDim, int colDim, int widthLength, int heightLength)
    {
        _rowDim = rowDim;
        _colDim = colDim;
        _widthLength = widthLength;
        _heightLength = heightLength;
        return this;
    }

    public RgvMapBuilder WithPoints(IEnumerable<PathPointDto> points)
    {
        _points = points;
        return this;
    }

    public RgvMapBuilder WithClusters(IEnumerable<ClusterDto> clusters)
    {
        _clusters = clusters;
        return this;
    }

    public RgvMapBuilder WithClusterFlows(IEnumerable<ClusterFlowDto> clusterFlows)
    {
        _clusterFlows = clusterFlows;
        return this;
    }

    public Result<RgvMap> Build()
    {
        var pathPointsResult = ToPathPoints(_points);
        if (pathPointsResult.IsFailed)
        {
            return AsValidation<RgvMap>(pathPointsResult);
        }

        List<PathPoint> pathPoints = pathPointsResult.Value;

        var pointLookup = pathPoints.ToDictionary(point => (point.RowPos, point.ColPos));

        var clustersResult = ResolveClusters(pointLookup);
        if (clustersResult.IsFailed)
        {
            return Result.Fail(clustersResult.Errors);
        }

        var clusterFlowsResult = ResolveClusterFlows(clustersResult.Value);
        if (clusterFlowsResult.IsFailed)
        {
            return Result.Fail(clusterFlowsResult.Errors);
        }

        var gridResult = Grid.Create(_rowDim, _colDim, _widthLength, _heightLength, pathPoints);
        if (gridResult.IsFailed)
        {
            return AsValidation<RgvMap>(gridResult);
        }

        var rgvMapResult = RgvMap.Create(gridResult.Value, clusterFlowsResult.Value);
        if (rgvMapResult.IsFailed)
        {
            return AsValidation<RgvMap>(rgvMapResult);
        }

        return Result.Ok(rgvMapResult.Value);
    }

    private Result<List<Cluster>> ResolveClusters(Dictionary<(int RowPos, int ColPos), PathPoint> pointLookup)
    {
        List<Cluster> resolvedClusters = [];

        foreach (var clusterDto in _clusters)
        {
            List<Station> stations = [];

            foreach (var position in clusterDto.Stations)
            {
                if (!pointLookup.TryGetValue((position.RowPos, position.ColPos), out var point))
                {
                    return Result.Fail(ApplicationError.Validation(
                        $"Cluster station at ({position.RowPos},{position.ColPos}) does not match any known point"));
                }

                if (point is not Station station)
                {
                    return Result.Fail(ApplicationError.Validation(
                        $"Point at ({position.RowPos},{position.ColPos}) is not a station"));
                }

                stations.Add(station);
            }

            var clusterResult = Cluster.Create(clusterDto.Name, clusterDto.ArrowColor, stations, []);
            if (clusterResult.IsFailed)
            {
                return AsValidation<List<Cluster>>(clusterResult);
            }

            resolvedClusters.Add(clusterResult.Value);
        }

        return Result.Ok(resolvedClusters);
    }

    private Result<List<ClusterFlow>> ResolveClusterFlows(List<Cluster> resolvedClusters)
    {
        List<ClusterFlow> resolvedClusterFlows = [];

        foreach (var clusterFlowDto in _clusterFlows)
        {
            List<Cluster> orderedClusters = [];

            foreach (var clusterIdx in clusterFlowDto.ClusterOrder)
            {
                if (clusterIdx < 0 || clusterIdx >= resolvedClusters.Count)
                {
                    return Result.Fail(ApplicationError.Validation(
                        $"A cluster flow references an out-of-range cluster index: {clusterIdx}"));
                }

                orderedClusters.Add(resolvedClusters[clusterIdx]);
            }

            var clusterFlowResult = ClusterFlow.Create(clusterFlowDto.ArrowColor, orderedClusters, []);
            if (clusterFlowResult.IsFailed)
            {
                return AsValidation<List<ClusterFlow>>(clusterFlowResult);
            }

            resolvedClusterFlows.Add(clusterFlowResult.Value);
        }

        return Result.Ok(resolvedClusterFlows);
    }

    private static Result<List<PathPoint>> ToPathPoints(IEnumerable<PathPointDto> points)
    {
        List<PathPoint> pathPoints = [];

        foreach (var point in points)
        {
            var pointResult = PointFactory.Create(
                PointCategory.FromString(point.Category),
                point.Position.RowPos,
                point.Position.ColPos,
                point.Name,
                point.Time);

            if (pointResult.IsFailed)
            {
                return Result.Fail(pointResult.Errors);
            }

            pathPoints.Add(pointResult.Value);
        }

        return Result.Ok(pathPoints);
    }

    private static Result<T> AsValidation<T>(ResultBase failed)
    {
        var error = failed.Errors[0];
        string detail = error.Metadata.TryGetValue("detail", out var value) ? value?.ToString() ?? "" : "";

        return Result.Fail<T>(new ApplicationError(error.Message, "Validation", detail));
    }
}
