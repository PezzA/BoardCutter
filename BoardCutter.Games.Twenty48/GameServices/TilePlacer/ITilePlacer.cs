using BoardCutter.Core;
using BoardCutter.Games.Twenty48.Game;

namespace BoardCutter.Games.Twenty48.GameServices.TilePlacer;

public interface ITilePlacer
{
    (Point2D, int) PlaceTile(Dictionary<int, NumberCell> grid, int gridSize);
}