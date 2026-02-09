using Domain.Missions.ValueObjects;

namespace Application.Common.Utilities;

public static class RouteIntersection
{
    public static List<PathPoint> GetIntersectionPathPoints(List<List<PathPoint>> routes, bool includeSelfIntersections = true)
    {
        var intersections = new HashSet<PathPoint>();

        var segmentsList = routes.Select(ToSegments).ToList();

        for (int i = 0; i < segmentsList.Count; i++)
        {
            for (int j = i + 1; j < segmentsList.Count; j++)
            {
                FindIntersections(
                    segmentsList[i],
                    segmentsList[j],
                    intersections);
            }
        }

        if (includeSelfIntersections)
        {
            for (int i = 0; i < segmentsList.Count; i++)
            {
                FindSelfIntersections(
                    segmentsList[i],
                    intersections);
            }
        }

        return [.. intersections];
    }

    private static List<(PathPoint, PathPoint)> ToSegments(List<PathPoint> route)
    {
        var segments = new List<(PathPoint, PathPoint)>();

        for (int i = 0; i < route.Count - 1; i++)
        {
            segments.Add((route[i], route[i + 1]));
        }

        return segments;
    }

    private static void FindIntersections(
        List<(PathPoint Start, PathPoint End)> segs1,
        List<(PathPoint Start, PathPoint End)> segs2,
        HashSet<PathPoint> intersections)
    {
        foreach (var seg1 in segs1)
        {
            foreach (var seg2 in segs2)
            {
                if (TryGetIntersection(seg1, seg2, out PathPoint? intersection) && intersection is not null)
                {
                    intersections.Add(intersection);
                }
            }
        }
    }

    private static void FindSelfIntersections(
        List<(PathPoint Start, PathPoint End)> segs,
        HashSet<PathPoint> intersections)
    {
        for (int i = 0; i < segs.Count; i++)
        {
            for (int j = i + 2; j < segs.Count; j++)
            {
                if (TryGetIntersection(segs[i], segs[j], out PathPoint? intersection) && intersection is not null)
                {
                    intersections.Add(intersection);
                }
            }
        }
    }

    private static bool TryGetIntersection(
        (PathPoint p1, PathPoint p2) seg1,
        (PathPoint p3, PathPoint p4) seg2,
        out PathPoint? intersection)
    {
        intersection = null;

        var (p1, p2) = seg1;
        var (p3, p4) = seg2;

        long denom = (long)(p1.RowPos - p2.RowPos) * (p3.ColPos - p4.ColPos)
                    - (long)(p1.ColPos - p2.ColPos) * (p3.RowPos - p4.RowPos);

        if (denom == 0) return false;

        long tNum = (long)(p1.RowPos - p3.RowPos) * (p3.ColPos - p4.ColPos)
                    - (long)(p1.ColPos - p3.ColPos) * (p3.RowPos - p4.RowPos);
        long uNum = -(long)(p1.RowPos - p2.RowPos) * (p1.ColPos - p3.ColPos)
                    + (long)(p1.ColPos - p2.ColPos) * (p1.RowPos - p3.RowPos);

        if ((denom > 0 && tNum >= 0 && tNum <= denom && uNum >= 0 && uNum <= denom) ||
            (denom < 0 && tNum <= 0 && tNum >= denom && uNum <= 0 && uNum >= denom))
        {
            long dx = (long)(p2.RowPos - p1.RowPos);
            long dy = (long)(p2.ColPos - p1.ColPos);
            long numRow = (long)p1.RowPos * denom + tNum * dx;
            long numCol = (long)p1.ColPos * denom + tNum * dy;

            if (numRow % denom == 0 && numCol % denom == 0)
            {
                int x = (int)(numRow / denom);
                int y = (int)(numCol / denom);

                intersection = PathPoint.Path(x, y);
                return true;
            }
        }

        return false;
    }
}