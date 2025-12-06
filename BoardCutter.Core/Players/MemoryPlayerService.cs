using System.Collections.Concurrent;

namespace BoardCutter.Core.Players;

public class MemoryPlayerService : IPlayerService
{
    private readonly ConcurrentDictionary<string, Player> _playerList = new();

    public Task<Player> AddOrUpdatePlayer(string userName, string connectionId, bool shouldExist)
    {
        var player = _playerList.AddOrUpdate(
            userName,
            // Add factory: create new player if doesn't exist
            _ => new Player(connectionId, userName),
            // Update factory: update existing player's connection ID
            (_, existingPlayer) =>
            {
                existingPlayer.ConnectionId = connectionId;
                return existingPlayer;
            });

        return Task.FromResult(player);
    }

    public Task<Player?> GetPlayerByConnectionId(string id)
    {
        var player = _playerList.Values.SingleOrDefault(p => p?.ConnectionId == id);

        return Task.FromResult<Player?>(player);
    }

    public Task<Player?> GetPlayerByUser(string user)
    {
        _playerList.TryGetValue(user, out var player);
        return Task.FromResult<Player?>(player);
    }
}