using BoardCutter.Core;
using BoardCutter.Core.Exceptions;
using BoardCutter.Games.Twenty48.Standard;

namespace BoardCutter.Games.Twenty48.Server;

/// <summary>
/// Random Tile Placer will place a tile in any available cell, 80% change of a 2, 20% change of a 4.
/// </summary>
public class RandomTilePlacer : ITilePlacer
{
    private readonly Random _rand = new();

    public (Point2D, int) PlaceTile(Dictionary<int, NumberCell> grid, int gridSize)
    {
        int select = _rand.Next(10);

        int tile = select == 0
            ? 4
            : 2;

        var candidates = new List<Point2D>();
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                var point = new Point2D(x, y);
                
                var cell = GridExtensions.GetByPos(point, grid);

                if (cell == null)
                {
                    candidates.Add(point);
                }
            }
        }
        
        if (candidates.Count == 0)
        {
            throw new InvalidGameStateException("Tried to get tile for full board");
        }

        int index = _rand.Next(candidates.Count);

        return (candidates[index], tile);
    }
}