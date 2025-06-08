using BoardCutter.Core;

namespace BoardCutter.Games.Twenty48;

public interface ITilePlacer
{
    (Point2D, int) PlaceTile(Dictionary<int, NumberCell> grid, int gridSize);
}