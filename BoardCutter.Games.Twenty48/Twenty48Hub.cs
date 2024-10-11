using Akka.Actor;
using Akka.Hosting;

using BoardCutter.Core.Actors;
using BoardCutter.Core.Players;
using BoardCutter.Games.Twenty48.Server.Actors;
using BoardCutter.Games.Twenty48.Standard;

using Microsoft.AspNetCore.SignalR;

namespace BoardCutter.Games.Twenty48.Server
{
    public record GetBasicDetailsResult(bool Success, Player Player, IActorRef? GameActor);

    public class Twenty48Hub(IRequiredActor<GameManager> gameManagerActor, IPlayerService playerService) : Hub
    {
        private readonly IActorRef _gameManagerActor = gameManagerActor.ActorRef;

        private async Task<GetBasicDetailsResult> GetBasicDetails(string gameId)
        {
            var loggedInUserName = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(loggedInUserName)) throw new InvalidDataException("Should have a user");

            var player = await playerService.AddOrUpdatePlayer(loggedInUserName, Context.ConnectionId, true);

            if (await _gameManagerActor.Ask(new GameManagerMessages.GetGameRequest(gameId),
                    TimeSpan.FromMilliseconds(100)) is not
                GameManagerMessages.GetGameDetails gameActorResp)
            {
                await Clients.Caller.SendAsync("Could not find requested game");
                return new GetBasicDetailsResult(false, new Player(string.Empty, String.Empty, string.Empty), null);
            }

            return new GetBasicDetailsResult(true, player, gameActorResp.GameActor);
        }

        public async Task AckHome(string message) {
            var loggedInUserName = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(loggedInUserName)) throw new InvalidDataException("Should have a user");

            var player = await playerService.AddOrUpdatePlayer(loggedInUserName, Context.ConnectionId, true);

            await Clients.Caller.SendAsync("Pong", $"ponging your {message}, {loggedInUserName}.");
        }

        public async Task InitGame(string gameId)
        {
            var requestDetails = await GetBasicDetails(gameId);

            if (requestDetails is not { Success: true } ||
                requestDetails.GameActor is null)
            {
                return;
            }

            requestDetails.GameActor.Tell(new GameMessages.BroadcastRequest(requestDetails.Player));
        }

        public async Task StartNew()
        {
            var player = await playerService.GetPlayerByConnectionId(Context.ConnectionId);

            if (player == null)
            {
                return;
            }

            _gameManagerActor.Tell(new GameManagerMessages.CreateGameRequest(player, "2048"));
        }

        public async Task SetupGame(string gameId, int size)
        {
            var requestDetails = await GetBasicDetails(gameId);

            if (requestDetails is not { Success: true } ||
                requestDetails.GameActor is null)
            {
                return;
            }

            requestDetails.GameActor.Tell(new GameMessages.SetupGameRequest(requestDetails.Player, size));
        }

        public async Task StartGame(string gameId)
        {
            var requestDetails = await GetBasicDetails(gameId);

            if (requestDetails is not { Success: true } ||
                requestDetails.GameActor is null)
            {
                return;
            }

            requestDetails.GameActor.Tell(new GameMessages.StartGameRequest(requestDetails.Player));
        }

        public async Task LeaveGame(string gameId)
        {
            var requestDetails = await GetBasicDetails(gameId);

            if (requestDetails is not { Success: true } ||
                requestDetails.GameActor is null)
            {
                return;
            }

            requestDetails.GameActor.Tell(new GameMessages.LeaveGameRequest(requestDetails.Player));
        }
        public async Task Move(string gameId, Direction direction)
        {
            var requestDetails = await GetBasicDetails(gameId);

            if (requestDetails is not { Success: true } ||
                requestDetails.GameActor is null)
            {
                return;
            }

            requestDetails.GameActor.Tell(new GameMessages.MoveRequest(requestDetails.Player, direction));
        }
    }
}
