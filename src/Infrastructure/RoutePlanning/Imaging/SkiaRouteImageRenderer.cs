using Application.Common.Interfaces.RoutePlanning;
using Domain.Missions.ValueObjects;

namespace Infrastructure.RoutePlanning.Imaging;

public class SkiaRouteImageRenderer : IRouteImageRenderer
{
    public byte[] Render(byte[] imageBytes, Grid grid, List<(List<PathPoint> Solution, string ArrowColor)> routes)
    {
        if (routes.Count == 0)
            throw new ArgumentException("No route details provided");

        using var drawer = new RouteDrawer(imageBytes, grid);

        foreach (var (solution, arrowColor) in routes)
        {
            drawer.DrawSolution(solution, arrowColor);
        }

        drawer.DrawStations(GetStations(grid));

        return drawer.Encode();
    }

    private static IEnumerable<Station> GetStations(Grid grid)
    {
        for (int row = 0; row < grid.RowDim; row++)
        {
            for (int col = 0; col < grid.ColDim; col++)
            {
                if (grid[row, col] is Station station)
                {
                    yield return station;
                }
            }
        }
    }
}
