using Domain.Missions.ValueObjects;
using static Domain.Missions.ValueObjects.PathPoint;

namespace Infrastructure.RoutePlanning.Rgv;

public static class PostProcessingRoute
{
    public static List<PathPoint> SmoothAndRasterizeFourDirections(
        List<PathPoint> originalPath,
        RgvMap map)
    {
        if (originalPath.Count < 3) 
            return originalPath;

        var sparseWaypoints = GetSparseStraightWaypoints(originalPath, map);

        var densePath = new List<PathPoint>
        {
            sparseWaypoints[0]
        };

        for (int i = 0; i < sparseWaypoints.Count - 1; i++)
        {
            var start = sparseWaypoints[i];
            var end   = sparseWaypoints[i + 1];

            var segment = GetAxisAlignedDensePath(start, end, map);

            for (int k = 1; k < segment.Count; k++)
                densePath.Add(segment[k]);
        }

        return densePath;
    }
    private static List<PathPoint> GetSparseStraightWaypoints(List<PathPoint> path, RgvMap map)
    {
        var waypoints = new List<PathPoint> { path[0] };
        int lastKeep = 0;

        for (int i = 1; i < path.Count; i++)
        {
            bool sameRow = path[lastKeep].RowPos == path[i].RowPos;
            bool sameCol = path[lastKeep].ColPos == path[i].ColPos;

            if (!sameRow && !sameCol)
            {
                waypoints.Add(path[i - 1]);
                lastKeep = i - 1;
                continue;
            }

            if (!IsAxisAlignedLineClear(path[lastKeep], path[i], map))
            {
                waypoints.Add(path[i - 1]);
                lastKeep = i - 1;
            }
        }

        if (!waypoints.Last().Equals(path.Last()))
            waypoints.Add(path.Last());

        return waypoints;
    }
    private static List<PathPoint> GetAxisAlignedDensePath(PathPoint start, PathPoint end, RgvMap map)
    {
        var points = new List<PathPoint>();

        int currentX = start.ColPos;
        int currentY = start.RowPos;

        int dx = end.ColPos - start.ColPos;
        int stepX = Math.Sign(dx);

        while (currentX != end.ColPos)
        {
            currentX += stepX;
            var p = map.GetPointAt(currentY, currentX);
            if (p is not null) points.Add(p);
        }

        
        int dy = end.RowPos - currentY;
        int stepY = Math.Sign(dy);

        while (currentY != end.RowPos)
        {
            currentY += stepY;
            var p = map.GetPointAt(currentY, currentX);
            if (p is not null) points.Add(p);
        }

        return points;
    }
    private static bool IsAxisAlignedLineClear(PathPoint a, PathPoint b, RgvMap map)
    {
        if (a.RowPos == b.RowPos) // horizontal
        {
            int minCol = Math.Min(a.ColPos, b.ColPos);
            int maxCol = Math.Max(a.ColPos, b.ColPos);
            for (int c = minCol; c <= maxCol; c++)
            {
                var p = map.GetPointAt(a.RowPos, c);
                if (p is null || p.Category == PointCategory.Obstacle)
                    return false;
            }
            return true;
        }
        else if (a.ColPos == b.ColPos) // vertical
        {
            int minRow = Math.Min(a.RowPos, b.RowPos);
            int maxRow = Math.Max(a.RowPos, b.RowPos);
            for (int r = minRow; r <= maxRow; r++)
            {
                var p = map.GetPointAt(r, a.ColPos);
                if (p is null || p.Category == PointCategory.Obstacle)
                    return false;
            }
            return true;
        }

        return false;
    }
}