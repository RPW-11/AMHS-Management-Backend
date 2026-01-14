using System.Text.Json;
using SkiaSharp;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    private const float ThicknessMultiplier = 0.02f;
    private const float ArrowThicknessControl = 6f;
    private const string LocalRoutePlanningDirectory = ".";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string DrawImage(MemoryStream imageStream, RoutePlanningDetailDto routePlanningDetailDto)
    {
        RgvMap rgvMap = routePlanningDetailDto.RgvMap;

        using var original = SKBitmap.Decode(imageStream);
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
            Color = new SKColor(173, 255, 47),
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

        string outputPath = Path.Combine(LocalRoutePlanningDirectory, $"{routePlanningDetailDto.Id}.png");
        Directory.CreateDirectory(LocalRoutePlanningDirectory);

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
    public IEnumerable<PathPoint> Solve(RoutePlanningDetailDto routePlanningDetailDto,
                                        RoutePlanningAlgorithm routePlanningAlgorithm,
                                        List<List<PathPoint>> sampleSolutions)
    {
        RgvMap rgvMap = routePlanningDetailDto.RgvMap;

        if (routePlanningAlgorithm == RoutePlanningAlgorithm.Dfs)
        {
            return DfsSolver.FindBestRoute(rgvMap);
        }
        if (routePlanningAlgorithm == RoutePlanningAlgorithm.GeneticAlgorithm)
        {
            var gaSolver = new GeneticAlgorithmSolver(rgvMap);
            return gaSolver.Solve(sampleSolutions);
        }

        throw new Exception($"Algorithm '{routePlanningAlgorithm}' is not implemented");
    }

    public string WriteToJson(RoutePlanningDetailDto routePlanningDetailDto)
    {
        try
        {
            string stringJson = JsonSerializer.Serialize(routePlanningDetailDto, _jsonSerializerOptions);
            File.WriteAllText($"{LocalRoutePlanningDirectory}\\{routePlanningDetailDto.Id}.json", stringJson);
            return $"{LocalRoutePlanningDirectory}\\{routePlanningDetailDto.Id}.json";
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
