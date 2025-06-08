using BoardCutter.Core.Players;

namespace BoardCutter.Games.Twenty48;

public class GameMessages
{
    // Setup game properties prior to stating
    public record SetupGameRequest(Player Player, int GridSize);
    // Start the game off!
    public record StartGameRequest(Player Player);
    // Log a move
    public record MoveRequest(Player Player, Direction Direction);
    public record BroadcastRequest(Player Player);
    public record LeaveGameRequest(Player Player);

}