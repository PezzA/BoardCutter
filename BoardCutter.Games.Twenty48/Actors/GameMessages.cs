using BoardCutter.Core.Players;
using BoardCutter.Games.Twenty48.Standard;

namespace BoardCutter.Games.Twenty48.Server.Actors;

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