using Akka.Actor;
using Akka.Hosting;

using BoardCutter.Core.Actors;
using BoardCutter.Core.Players;

using Microsoft.AspNetCore.SignalR;

namespace BoardCutter.Games.Twenty48
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
                return new GetBasicDetailsResult(false, new Player(string.Empty, string.Empty, string.Empty), null);
            }

            return new GetBasicDetailsResult(true, player, gameActorResp.GameActor);
        }

        /// <summary>
        /// CheckPlayerStatus checks to see if both the player and the game exist.  If so broadcast
        /// latest status to the game clients.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task CheckPlayerStatus(string gameId)
        {
            var loggedInUserName = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(loggedInUserName)) throw new InvalidDataException("Should have a user");

            var player = await playerService.AddOrUpdatePlayer(loggedInUserName, Context.ConnectionId, true);

            if (player is null)
            {
                await Clients.Caller.SendAsync("PlayerStatus", "Player Not Found");
                return;
            }

            await Clients.Caller.SendAsync("PlayerStatus", "Player Found and Connected");

            if (string.IsNullOrEmpty(gameId)) return;

            var requestDetails = await GetBasicDetails(gameId);

            requestDetails.GameActor.Tell(new GameMessages.BroadcastRequest(requestDetails.Player));
        }

        /// <summary>
        /// Request to start a new game, significantly, this request goes to the game manager to create the game.
        /// </summary>
        /// <returns></returns>
        public async Task StartNew()
        {
            var player = await playerService.GetPlayerByConnectionId(Context.ConnectionId);

            if (player == null)
            {
                return;
            }

            _gameManagerActor.Tell(new GameManagerMessages.CreateGameRequest(player, "2048"));
        }

        /// <summary>
        /// Process a move.  Main gameplay logic method
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
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
