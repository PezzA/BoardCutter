using BoardCutter.Core;

namespace BoardCutter.Games.Twenty48.Standard
{
    public record PublicVisible(string GameId, int Score, GameStatus Status, int GridSize, NumberCell[] Cells);
}
