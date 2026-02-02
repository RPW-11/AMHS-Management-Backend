using Domain.Missions.ValueObjects;
using SkiaSharp;

public static class RouteDrawer
{
    private const float ThicknessMultiplier = 0.02f;
    private const float ArrowThicknessControl = 6f;
    private const int ImageQuality = 92;

    public static byte[] DrawMultipleRoutes (
        byte[] imageBytes,
        List<string> hexColors,
        List<RgvMap> mapsWithSolutions,
        List<PathPoint> intersections
    )
    {
        using var stream = new MemoryStream(imageBytes);
        using var original = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Failed to decode base image");
        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);       
        canvas.DrawBitmap(original, 0, 0);

        var firstMap = mapsWithSolutions.First();
        float cellWidth  = (float)original.Width  / firstMap.ColDim;
        float cellHeight = (float)original.Height / firstMap.RowDim;

        float penThickness = Math.Max(1.5f, MathF.Round(ThicknessMultiplier * Math.Min(cellWidth, cellHeight)));
        float arrowSize    = ArrowThicknessControl * penThickness;

        int arrowInterval = Math.Max(1, 10);

        for (int i = 0; i < mapsWithSolutions.Count; i++)
        {
            var rgvMap = mapsWithSolutions.ElementAt(i);
            string hexColor = hexColors[i];

            if (!SKColor.TryParse(hexColor, out SKColor routeColor))
            {
                routeColor = SKColors.Black;
            }

            var points = new List<SKPoint>();
            foreach (var p in rgvMap.Solutions)
            {
                float x = p.ColPos * cellWidth + cellWidth / 2f;
                float y = p.RowPos * cellHeight + cellHeight / 2f;
                points.Add(new SKPoint(x, y));
            }

            if (points.Count < 2) continue;

            using var linePaint = new SKPaint
            {
                Style       = SKPaintStyle.Stroke,
                Color       = routeColor,
                StrokeWidth = penThickness,
                IsAntialias = true,
                StrokeCap   = SKStrokeCap.Round,
                StrokeJoin  = SKStrokeJoin.Round
            };

            using var path = new SKPath();
            path.MoveTo(points[0]);
            for (int j = 1; j < points.Count; j++)
                path.LineTo(points[j]);

            canvas.DrawPath(path, linePaint);

            using var arrowFill = new SKPaint
            {
                Style       = SKPaintStyle.Fill,
                Color       = routeColor,
                IsAntialias = true
            };

            using var arrowStroke = new SKPaint
            {
                Style       = SKPaintStyle.Stroke,
                Color       = SKColors.Black.WithAlpha(180),
                StrokeWidth = Math.Max(1, penThickness * 0.35f),
                IsAntialias = true
            };

            // Place arrows more frequently on shorter paths, less on very long ones
            int dynamicInterval = Math.Max(1, points.Count / 12);

            for (int j = 0; j < points.Count - 1; j += dynamicInterval)
            {
                var curr = points[j];
                var next = points[j + 1];
                DrawArrow(canvas, curr, next, arrowSize, arrowFill, arrowStroke);
            }

            if (points.Count >= 2)
            {
                DrawArrow(canvas, points[^2], points[^1], arrowSize, arrowFill, arrowStroke);
            }
        }

        DrawIntersections(canvas, intersections, firstMap, original.Width, original.Height);

        using var finalImage = surface.Snapshot();
        using var data = finalImage.Encode(SKEncodedImageFormat.Png, ImageQuality);

        return data.ToArray();
    }

    private static void DrawArrow(SKCanvas canvas, SKPoint start, SKPoint end, 
                             float size, SKPaint fillPaint, SKPaint strokePaint)
    {
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < 0.001f) return;

        dx /= len;
        dy /= len;

        // Perpendicular vector
        float px = -dy;
        float py = dx;

        SKPoint tip = end;

        SKPoint left = new(
            end.X - dx * size + px * size * 0.5f,
            end.Y - dy * size + py * size * 0.5f);

        SKPoint right = new(
            end.X - dx * size - px * size * 0.5f,
            end.Y - dy * size - py * size * 0.5f);

        using var arrowPath = new SKPath();
        arrowPath.MoveTo(tip);
        arrowPath.LineTo(left);
        arrowPath.LineTo(right);
        arrowPath.Close();

        canvas.DrawPath(arrowPath, fillPaint);
        canvas.DrawPath(arrowPath, strokePaint);
    }

    private static void DrawIntersections(
        SKCanvas canvas,
        List<PathPoint> intersections,
        RgvMap rgvMap,
        float imageWidth,
        float imageHeight,
        float circleRadiusMultiplier = 0.38f)
    {
        if (intersections.Count == 0)
            return;

        float cellWidth  = imageWidth  / rgvMap.ColDim;
        float cellHeight = imageHeight / rgvMap.RowDim;
        float baseSize   = Math.Min(cellWidth, cellHeight);
        float radius     = baseSize * circleRadiusMultiplier;

        float strokeWidth = Math.Max(2f, baseSize * 0.08f);

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,        
            StrokeWidth = strokeWidth,
            IsAntialias = true
        };

        foreach (var point in intersections)
        {
            float x = point.ColPos * cellWidth  + cellWidth  / 2f;
            float y = point.RowPos * cellHeight + cellHeight / 2f;

            if (x < 0 || y < 0 || x >= imageWidth || y >= imageHeight)
                continue;

            canvas.DrawCircle(x, y, radius, strokePaint);
        }
    }
}