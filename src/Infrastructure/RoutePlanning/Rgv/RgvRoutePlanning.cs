using System.Text.Json;
using SkiaSharp;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Microsoft.Extensions.Options;
using Domain.Missions.ValueObjects;
using Domain.Missions;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning(IOptions<RoutePlanningSettings> routePlanningSetting) : IRgvRoutePlanning
{
    private const float ThicknessMultiplier = 0.02f;
    private const float ArrowThicknessControl = 6f;
    private readonly string _localRoutePlanningDirectory = routePlanningSetting.Value.LocalDirectory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public string DrawImage(byte[] imageBytes, string hexColor, RgvMap rgvMap, string name)
    {
        using var stream = new MemoryStream(imageBytes);
        using var original = SKBitmap.Decode(stream);
        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        using var canvas = surface.Canvas;

        canvas.DrawBitmap(original, 0, 0);

        float cellWidth = (float)original.Width / rgvMap.ColDim;
        float cellHeight = (float)original.Height / rgvMap.RowDim;

        float penThickness = Math.Max(1, MathF.Round(ThicknessMultiplier * Math.Min(cellWidth, cellHeight)));

        var points = new List<SKPoint>();
        foreach (var p in rgvMap.Solutions)
        {
            float x = p.ColPos * cellWidth + cellWidth / 2f;
            float y = p.RowPos * cellHeight + cellHeight / 2f;
            points.Add(new SKPoint(x, y));
        }

        if (points.Count < 2) 
        {
            throw new Exception("Not enough points to draw a route.");
        }

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = penThickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        using var path = new SKPath();
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++)
            path.LineTo(points[i]);

        canvas.DrawPath(path, paint);

        float arrowSizeMultiplier = ArrowThicknessControl;

        int arrowInterval = Math.Max(1, points.Count / 10);

        using var arrowPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColor.TryParse(hexColor, out var color) ? color : SKColors.Black,
            IsAntialias = true
        };

        using var arrowStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = Math.Max(1, penThickness * 0.4f),
            IsAntialias = true
        };

        for (int i = 0; i < points.Count - 1; i += arrowInterval)
        {
            var curr = points[i];
            var next = points[i + 1];
            DrawArrow(canvas, curr, next, arrowSizeMultiplier * penThickness, arrowPaint, arrowStroke);
        }

        if (points.Count >= 2)
        {
            DrawArrow(canvas, points[^2], points[^1], 
                    arrowSizeMultiplier * penThickness, arrowPaint, arrowStroke);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        string outputPath = Path.Combine(_localRoutePlanningDirectory, $"{name}.png");
        Directory.CreateDirectory(_localRoutePlanningDirectory);

        File.WriteAllBytes(outputPath, data.ToArray());

        return outputPath;
    }

    public string DrawMultipleFlows(
        byte[] imageBytes,
        List<string> colors,
        RoutePlanningMission routePlanningMission,
        string suffix = "")
    {
        if (!routePlanningMission.RgvMaps.Any())
            throw new ArgumentException("No route details provided");

        if (colors == null || colors.Count != routePlanningMission.RgvMaps.Count())
            throw new ArgumentException("Number of colors must match number of route details");

        
        using var stream = new MemoryStream(imageBytes);
        using var original = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("Failed to decode base image");
        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);       
        canvas.DrawBitmap(original, 0, 0);

        var firstMap = routePlanningMission.RgvMaps.First();
        float cellWidth  = (float)original.Width  / firstMap.ColDim;
        float cellHeight = (float)original.Height / firstMap.RowDim;

        float penThickness = Math.Max(1, MathF.Round(ThicknessMultiplier * Math.Min(cellWidth, cellHeight)));
        float arrowSize    = ArrowThicknessControl * penThickness;

        int arrowInterval = Math.Max(1, 10);

        // Draw each route
        for (int i = 0; i < routePlanningMission.RgvMaps.Count(); i++)
        {
            var rgvMap = routePlanningMission.RgvMaps.ElementAt(i);
            string hexColor = colors[i];

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

        using var finalImage = surface.Snapshot();
        using var data = finalImage.Encode(SKEncodedImageFormat.Png, 92);

        string fileName = string.IsNullOrWhiteSpace(suffix) ? $"{routePlanningMission.Id}.png" : $"{routePlanningMission.Id}_{suffix}.png";
        string outputPath = Path.Combine(_localRoutePlanningDirectory, fileName);

        Directory.CreateDirectory(_localRoutePlanningDirectory);
        File.WriteAllBytes(outputPath, data.ToArray());

        return outputPath;
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
    public (IEnumerable<PathPoint>, IEnumerable<PathPoint>) Solve(RgvMap rgvMap,
                                        RoutePlanningAlgorithm routePlanningAlgorithm,
                                        List<List<PathPoint>> sampleSolutions)
    {

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.Dfs)
        {
            var result = DfsSolver.FindBestRoute(rgvMap);
            var preprocessedResult = PostProcessingRoute.SmoothAndRasterizeFourDirections(result, rgvMap);

            return (result, preprocessedResult);
        }
        if (routePlanningAlgorithm == RoutePlanningAlgorithm.GeneticAlgorithm)
        {
            var gaSolver = new GeneticAlgorithmSolver(rgvMap);
            var result = gaSolver.Solve(sampleSolutions);
            var preprocessedResult = PostProcessingRoute.SmoothAndRasterizeFourDirections(result, rgvMap);

            return (result, preprocessedResult);
        }

        throw new Exception($"Algorithm '{routePlanningAlgorithm}' is not implemented");
    }

    public string WriteToJson(RoutePlanningDetailDto routePlanningDetailDto)
    {
        try
        {
            string stringJson = JsonSerializer.Serialize(routePlanningDetailDto, _jsonSerializerOptions);
            string path = Path.Combine(_localRoutePlanningDirectory, routePlanningDetailDto.Id) + ".json";
            File.WriteAllText(path, stringJson);
            return path;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw new Exception (error.Message);
        }
    }

    public RoutePlanningSummaryDto ReadFromJson(string jsonFileUrl)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonFileUrl);
            RoutePlanningSummaryDto? routePlanningDetail = JsonSerializer.Deserialize<RoutePlanningSummaryDto>(jsonString) ?? throw new Exception("Error serializing the resource file");

            return routePlanningDetail;
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            throw new Exception(error.Message);
        }
    }

    public RoutePlanningScoreDto GetRouteScore(List<PathPoint> solution, RgvMap rgvMap)
    {
        var (throughput, trackLength, numOfRgvs) = RouteEvaluator.GetSolutionScores(solution, rgvMap);

        return new(throughput, trackLength, numOfRgvs);
    }
}
