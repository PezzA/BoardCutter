using BoardCutter.Core;
using BoardCutter.Games.Twenty48.Standard;

namespace BoardCutter.Games.Twenty48.Server;

public static class GridExtensions
{
    public static NumberCell? GetByPos(Point2D point, Dictionary<int, NumberCell> grid)
    {
        return grid.SingleOrDefault(c => c.Value.Point.X == point.X && c.Value.Point.Y == point.Y && !c.Value.Destroy).Value;
    }

    public static NumberCell? GetById(int id, Dictionary<int, NumberCell> grid)
    {
        return grid.ContainsKey(id)
            ? grid[id]
            : null;
    }


    public static int[][] ToJaggedArray(Dictionary<int, NumberCell> grid, int gridSize)
    {
        int[][] jaggedArray = new int[gridSize][];

        for (int i = 0; i < gridSize; i++)
        {
            jaggedArray[i] = new int[gridSize];
        }

        foreach (var cell in grid)
        {
            jaggedArray[cell.Value.Point.Y][cell.Value.Point.X] = cell.Value.Value;
        }

        return jaggedArray;
    }

}