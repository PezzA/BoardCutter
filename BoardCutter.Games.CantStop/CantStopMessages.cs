using BoardCutter.Core.Players;

namespace BoardCutter.Games.CantStop;

public class CantStopMessages
{
    public record SetupGameRequest(Player Player);
    public record JoinGameRequest(Player Player);
    public record LeaveGameRequest(Player Player);
    public record StartGameRequest(Player Player);
    public record MoveRequest(Player Player, int[] Moves);
}


