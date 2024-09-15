using BoardCutter.Core;
using BoardCutter.Games.Twenty48.Standard;

namespace BoardCutter.Games.Twenty48.Server;

public interface ITilePlacer
{
    (Point2D, int) PlaceTile(Dictionary<int, NumberCell> grid, int gridSize);
}