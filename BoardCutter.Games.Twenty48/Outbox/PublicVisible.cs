using BoardCutter.Core;
using BoardCutter.Games.Twenty48.Game;

namespace BoardCutter.Games.Twenty48.Outbox
{
    public record PublicVisible(string GameId, int Score, GameStatus Status, int GridSize, NumberCell[] Cells);
}
