using System.Drawing;
using System.Drawing.Imaging;
using Application.Common.Interfaces.RoutePlanning;
using Domain.Entities;
using Domain.ValueObjects.Mission.RoutePlanning;

namespace Infrastructure.RoutePlanning.Rgv;

public class RgvRoutePlanning : IRgvRoutePlanning
{
    public void DrawImage(MemoryStream imageStream, RgvMap rgvMap)
    {
        Image mapImage = Image.FromStream(imageStream);
        using (var graphics = Graphics.FromImage(mapImage))
        {
            // Set up drawing properties
            var pen = new Pen(Color.Red, 3); // Red line with 3px width

            // Calculate cell dimensions
            float cellWidth = (float)mapImage.Width / rgvMap.ColDim;
            float cellHeight = (float)mapImage.Height / rgvMap.RowDim;

            // Convert matrix coordinates to pixel positions (center of each cell)
            var points = new List<PointF>();
            foreach (var point in rgvMap.Solutions)
            {
                float x = point.ColPos * cellWidth + cellWidth / 2;
                float y = point.RowPos * cellHeight + cellHeight / 2;
                points.Add(new PointF(x, y));
            }

            // Draw connected lines
            if (points.Count > 1)
            {
                graphics.DrawLines(pen, points.ToArray());
            }

            // Save the result
            mapImage.Save("C:\\Users\\user\\Downloads\\resultt.png", ImageFormat.Png); // saved locally for now
        }
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
}
