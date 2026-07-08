using Domain.Missions.ValueObjects;
using SkiaSharp;

namespace Infrastructure.RoutePlanning.Rgv;

public sealed class RouteDrawer : IDisposable
{
    private const float ThicknessMultiplier = 0.02f;
    private const float ArrowThicknessControl = 6f;
    private const int ImageQuality = 92;

    private readonly SKBitmap _original;
    private readonly SKSurface _surface;
    private readonly SKCanvas _canvas;
    private readonly float _cellWidth;
    private readonly float _cellHeight;
    private readonly float _penThickness;
    private readonly float _arrowSize;

    public RouteDrawer(byte[] imageBytes, Grid grid)
    {
        using var stream = new MemoryStream(imageBytes);
        _original = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Failed to decode base image");
        _surface = SKSurface.Create(new SKImageInfo(_original.Width, _original.Height));
        _canvas = _surface.Canvas;
        _canvas.Clear(SKColors.Transparent);
        _canvas.DrawBitmap(_original, 0, 0);

        _cellWidth = (float)_original.Width / grid.ColDim;
        _cellHeight = (float)_original.Height / grid.RowDim;
        _penThickness = Math.Max(2f, MathF.Round(ThicknessMultiplier * Math.Min(_cellWidth, _cellHeight)));
        _arrowSize = ArrowThicknessControl * _penThickness;
    }

    public void DrawSolution(List<PathPoint> solution, string arrowColor)
    {
        if (!SKColor.TryParse(arrowColor, out SKColor routeColor))
        {
            routeColor = SKColors.Black;
        }

        var points = new List<SKPoint>();
        foreach (var p in solution)
        {
            float x = p.ColPos * _cellWidth + _cellWidth / 2f;
            float y = p.RowPos * _cellHeight + _cellHeight / 2f;
            points.Add(new SKPoint(x, y));
        }

        if (points.Count < 2) return;

        using var linePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = routeColor,
            StrokeWidth = _penThickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        using var path = new SKPath();
        path.MoveTo(points[0]);
        for (int j = 1; j < points.Count; j++)
            path.LineTo(points[j]);

        _canvas.DrawPath(path, linePaint);

        using var arrowFill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = routeColor,
            IsAntialias = true
        };

        using var arrowStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black.WithAlpha(180),
            StrokeWidth = Math.Max(1, _penThickness * 0.35f),
            IsAntialias = true
        };

        // Place arrows more frequently on shorter paths, less on very long ones
        int dynamicInterval = Math.Max(1, points.Count / 12);

        for (int j = 0; j < points.Count - 1; j += dynamicInterval)
        {
            var curr = points[j];
            var next = points[j + 1];
            DrawArrow(curr, next, arrowFill, arrowStroke);
        }

        if (points.Count >= 2)
        {
            DrawArrow(points[^2], points[^1], arrowFill, arrowStroke);
        }
    }

    public void DrawStations(IEnumerable<Station> stations, SKColor? starColor = null)
    {
        SKColor color = starColor ?? SKColors.Gold;
        float baseSize = Math.Min(_cellWidth, _cellHeight);
        float outerRadius = baseSize * 0.35f;
        float innerRadius = outerRadius * 0.5f;

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color,
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = Math.Max(1f, baseSize * 0.04f),
            IsAntialias = true
        };

        foreach (var station in stations)
        {
            float centerX = station.ColPos * _cellWidth + _cellWidth / 2f;
            float centerY = station.RowPos * _cellHeight + _cellHeight / 2f;

            using var starPath = BuildStarPath(centerX, centerY, outerRadius, innerRadius);
            _canvas.DrawPath(starPath, fillPaint);
            _canvas.DrawPath(starPath, strokePaint);
        }
    }

    private static SKPath BuildStarPath(float centerX, float centerY, float outerRadius, float innerRadius)
    {
        const int points = 5;
        const double startAngle = -Math.PI / 2;
        double angleStep = Math.PI / points;

        var path = new SKPath();

        for (int i = 0; i < points * 2; i++)
        {
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            double angle = startAngle + i * angleStep;
            float x = centerX + radius * (float)Math.Cos(angle);
            float y = centerY + radius * (float)Math.Sin(angle);

            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        path.Close();
        return path;
    }

    public byte[] Encode()
    {
        using var finalImage = _surface.Snapshot();
        using var data = finalImage.Encode(SKEncodedImageFormat.Png, ImageQuality);

        return data.ToArray();
    }

    private void DrawArrow(SKPoint start, SKPoint end, SKPaint fillPaint, SKPaint strokePaint)
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
            end.X - dx * _arrowSize + px * _arrowSize * 0.5f,
            end.Y - dy * _arrowSize + py * _arrowSize * 0.5f);

        SKPoint right = new(
            end.X - dx * _arrowSize - px * _arrowSize * 0.5f,
            end.Y - dy * _arrowSize - py * _arrowSize * 0.5f);

        using var arrowPath = new SKPath();
        arrowPath.MoveTo(tip);
        arrowPath.LineTo(left);
        arrowPath.LineTo(right);
        arrowPath.Close();

        _canvas.DrawPath(arrowPath, fillPaint);
        _canvas.DrawPath(arrowPath, strokePaint);
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _original?.Dispose();
    }
}
