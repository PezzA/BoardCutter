using Akka.Actor;
using Akka.Hosting;
using BoardCutter.Core.Actors;
using BoardCutter.Core.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoardCutter.Web.Hubs
{
    [Authorize]
    public class GameLobbyHub : Hub
    {
        private readonly IActorRef _gameManagerActor;
        private readonly IPlayerService _playerService;

        public GameLobbyHub(IRequiredActor<GameManager> gameManagerActor, IPlayerService playerService)
        {
            _gameManagerActor = gameManagerActor.ActorRef;
            _playerService = playerService;
        }

        public async Task JoinLobby()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "GameLobby");
            
            // Send current game list to the newly connected user
            await SendGameList();
        }

        public async Task LeaveLobby()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "GameLobby");
        }

        public async Task GetGameList()
        {
            await SendGameList();
        }

        public async Task CreateGame(string gameTag)
        {
            // Get player information from claims
            var boardCutterId = Context.User?.FindFirst("BoardCutterId")?.Value;
            var userName = Context.User?.Identity?.Name ?? $"Anonymous-{boardCutterId?[..8]}";

            if (string.IsNullOrEmpty(boardCutterId))
            {
                await Clients.Caller.SendAsync("Error", "Unable to identify user");
                return;
            }

            var player = await _playerService.AddOrUpdatePlayer(userName, Context.ConnectionId, true);

            // Request game creation from GameManager
            _gameManagerActor.Tell(new GameManagerMessages.CreateGameRequest(player, gameTag));

            // Game list will be updated automatically when GameCreated notification is received
        }

        public async Task SendGameList()
        {
            try
            {
                var gameListResponse = await _gameManagerActor.Ask(new GameManagerMessages.GetGameList(), TimeSpan.FromSeconds(5));
                
                if (gameListResponse is GameManagerNotifications.BaseGameNotification[] gameList)
                {
                    await Clients.Caller.SendAsync("GameListUpdated", gameList);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to get game list: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "GameLobby");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
