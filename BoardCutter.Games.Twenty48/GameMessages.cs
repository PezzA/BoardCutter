using BoardCutter.Core.Players;

namespace BoardCutter.Games.Twenty48;

public class GameMessages
{
    public record SetupGameRequest(Player Player, int GridSize);
    public record StartGameRequest(Player Player);
    public record MoveRequest(Player Player, Direction Direction);
    public record BroadcastRequest(Player Player);
    public record LeaveGameRequest(Player Player);
}