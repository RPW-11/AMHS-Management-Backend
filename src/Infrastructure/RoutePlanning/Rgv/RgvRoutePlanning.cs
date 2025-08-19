using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Mission.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    private const float ThicknessMultiplier = 0.02f;
    private const float ArrowThicknessControl = 6f;
    private const string LocalRoutePlanningDirectory = "C:\\Users\\user\\Downloads\\RoutePlanningResults";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string DrawImage(MemoryStream imageStream, RoutePlanningDetailDto routePlanningDetailDto)
    {
        RgvMap rgvMap = routePlanningDetailDto.RgvMap;
        Image mapImage = Image.FromStream(imageStream);
        using (var graphics = Graphics.FromImage(mapImage))
        {
            float cellWidth = (float)mapImage.Width / rgvMap.ColDim;
            float cellHeight = (float)mapImage.Height / rgvMap.RowDim;

            var penThickness = (float)Math.Max(1, Math.Round(ThicknessMultiplier * Math.Min(cellWidth, cellHeight)));

            var pen = new Pen(Color.Black, penThickness); // 3px width
            var arrowBrush = new SolidBrush(Color.YellowGreen);

            var points = new List<PointF>();
            foreach (var point in rgvMap.Solutions)
            {
                float x = point.ColPos * cellWidth + cellWidth / 2;
                float y = point.RowPos * cellHeight + cellHeight / 2;
                points.Add(new PointF(x, y));
            }

            if (points.Count > 1)
            {
                graphics.DrawLines(pen, points.ToArray());

                int arrowInterval = Math.Max(1, points.Count / 10);

                for (int i = 0; i < points.Count - 1; i += arrowInterval)
                {
                    PointF currentPoint = points[i];
                    PointF nextPoint = points[i + 1];

                    float dx = nextPoint.X - currentPoint.X;
                    float dy = nextPoint.Y - currentPoint.Y;

                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (length > 0)
                    {
                        dx /= length;
                        dy /= length;
                    }

                    PointF arrowPosition = new PointF(
                        (currentPoint.X + nextPoint.X) / 2,
                        (currentPoint.Y + nextPoint.Y) / 2);

                    DrawFilledArrow(graphics, arrowBrush, arrowPosition, dx, dy, ArrowThicknessControl * penThickness);
                }

                if (points.Count > 1)
                {
                    PointF lastPoint = points[points.Count - 2];
                    PointF endPoint = points[points.Count - 1];

                    float dx = endPoint.X - lastPoint.X;
                    float dy = endPoint.Y - lastPoint.Y;

                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (length > 0)
                    {
                        dx /= length;
                        dy /= length;
                    }

                    DrawFilledArrow(graphics, arrowBrush, endPoint, dx, dy, ArrowThicknessControl * penThickness);
                }
            }

            // Save the result
            mapImage.Save($"{LocalRoutePlanningDirectory}\\{routePlanningDetailDto.Id}.png", ImageFormat.Png); // saved locally for now
            return $"{LocalRoutePlanningDirectory}\\{routePlanningDetailDto.Id}.png";
        }
    }
    
    private static void DrawFilledArrow(Graphics g, Brush brush, PointF position, float dx, float dy, float size)
    {
        float px = -dy;
        float py = dx;

        PointF arrowTip = position;
        PointF arrowLeft = new PointF(
            position.X - dx * size + px * size / 2,
            position.Y - dy * size + py * size / 2);
        PointF arrowRight = new PointF(
            position.X - dx * size - px * size / 2,
            position.Y - dy * size - py * size / 2);

        PointF[] arrowPoints = [arrowTip, arrowLeft, arrowRight];

        g.FillPolygon(brush, arrowPoints);
        g.DrawPolygon(Pens.Black, arrowPoints);
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
            RoutePlanningSummaryDto? routePlanningSummaryDto = JsonSerializer.Deserialize<RoutePlanningSummaryDto>(jsonString) ?? throw new Exception("Error serializing the resource file");

            return routePlanningSummaryDto;
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
