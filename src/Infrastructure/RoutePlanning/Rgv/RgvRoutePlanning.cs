using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Application.Common.Interfaces.RoutePlanning;
using Domain.Mission.ValueObjects;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented=true };
    public void DrawImage(MemoryStream imageStream, RgvMap rgvMap)
    {
        Image mapImage = Image.FromStream(imageStream);
        using (var graphics = Graphics.FromImage(mapImage))
        {
            var pen = new Pen(Color.Black, 3); // 3px width
            var arrowBrush = new SolidBrush(Color.YellowGreen);

            float cellWidth = (float)mapImage.Width / rgvMap.ColDim;
            float cellHeight = (float)mapImage.Height / rgvMap.RowDim;

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

                    DrawFilledArrow(graphics, arrowBrush, arrowPosition, dx, dy, 10f);
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

                    DrawFilledArrow(graphics, arrowBrush, endPoint, dx, dy, 10f);
                }
            }

            // Save the result
            mapImage.Save("C:\\Users\\user\\Downloads\\resultt.png", ImageFormat.Png); // saved locally for now
        }
    }

    private void DrawFilledArrow(Graphics g, Brush brush, PointF position, float dx, float dy, float size)
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

    public IEnumerable<PathPoint> Solve(RgvMap rgvMap)
    {

        var route = DfsSolver.FindBestRoute(rgvMap);
        foreach (var p in route)
        {
            Console.Write($"({p.RowPos},{p.ColPos})->");
        }
        Console.WriteLine("");

        return route;
    }

    public void WriteToJson(RgvMap rgvMap)
    {
        try
        {
            string stringJson = JsonSerializer.Serialize(rgvMap, _jsonSerializerOptions);
            File.WriteAllText("C:\\Users\\user\\Downloads\\rgv_map.json", stringJson);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
        }
    }
}
