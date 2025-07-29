using System.Drawing;
using System.Drawing.Imaging;
using Application.Common.Interfaces.RoutePlanning;
using Domain.ValueObjects.Mission.RoutePlanning;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    public void DrawImage(MemoryStream imageStream, IEnumerable<(int rowPos, int colPos)> coordinates, int rowDim, int colDim)
    {
        Image mapImage = Image.FromStream(imageStream);
        using (var graphics = Graphics.FromImage(mapImage))
        {
            // Set up drawing properties
            var pen = new Pen(Color.Red, 3); // Red line with 3px width

            // Calculate cell dimensions
            float cellWidth = (float)mapImage.Width / colDim;
            float cellHeight = (float)mapImage.Height / rowDim;

            // Convert matrix coordinates to pixel positions (center of each cell)
            var points = new List<PointF>();
            foreach (var (row, col) in coordinates)
            {
                float x = col * cellWidth + cellWidth / 2;
                float y = row * cellHeight + cellHeight / 2;
                points.Add(new PointF(x, y));
            }

            // Draw connected lines
            if (points.Count > 1)
            {
                graphics.DrawLines(pen, points.ToArray());
            }

            // Save the result
        }
    }

    public IEnumerable<PathPoint> Solve(int rowDim, int colDim, IEnumerable<PathPoint> points, IEnumerable<(int rowPos, int colPos)> stationsOrder)
    {
        
        RgvMap rgvMap = RgvMap.Create(rowDim, colDim, [.. stationsOrder], [.. points]);

        var route = DfsSolver.FindBestRoute(rgvMap);
        foreach (var p in route)
        {
            Console.Write($"({p.RowPos},{p.ColPos})->");
        }
        Console.WriteLine("");

        return route;
    }
}
