using BoardCutter.Core;
using BoardCutter.Core.Exceptions;

namespace BoardCutter.Games.Twenty48;

/// <summary>
/// Predictable Tile Placer will always put a 2 in the first available slot reading from left to right, top to bottom
/// </summary>
public class PredictableTilePlacer : ITilePlacer
{
    public (Point2D, int) PlaceTile(Dictionary<int, NumberCell> grid, int gridSize)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                var point = new Point2D(x, y);
                var cell = GridExtensions.GetByPos(point, grid);

                if (cell == null)
                {
                    return (point, 2);
                }
            }
        }

        throw new InvalidGameStateException("Tried to get tile for full board");
    }
}