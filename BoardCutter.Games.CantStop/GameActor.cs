using Akka.Actor;

using BoardCutter.Core;
using BoardCutter.Core.Actors;
using BoardCutter.Core.Exceptions;
using BoardCutter.Core.Players;

namespace BoardCutter.Games.CantStop;

public record PublicVisible(string GameId, Player[] Players, BoardState BoardState);

public record ValidationWarning(string Message);

public class GameActor : ReceiveActor
{
    private Player? _owner;

    private List<Player> _players = [];

    private string _gameId = Guid.NewGuid().ToString();

    private readonly IDiceThrower _diceThrower;

    private GameStatus _gameStatus = GameStatus.SettingUp;

    private readonly IActorRef _hubWriter;

    private BoardState _boardState;

    private int _maxPlayers = 2;

    public GameActor(IActorRef hubWriter, IDiceThrower diceThrower)
    {
        _diceThrower = diceThrower;
        _hubWriter = hubWriter;

        Receive<GameManagerMessages.CreateGameSpecificRequest>(CreateGame);
        Receive<CantStopMessages.SetupGameRequest>(SetupGame);
        Receive<CantStopMessages.StartGameRequest>(StartGame);
        Receive<CantStopMessages.JoinGameRequest>(JoinGame);
        Receive<CantStopMessages.LeaveGameRequest>(LeaveGame);
        Receive<CantStopMessages.MoveRequest>(MoveGame);
    }

    private void MoveGame(CantStopMessages.MoveRequest request)
    {
        throw new NotImplementedException();
    }

    private void LeaveGame(CantStopMessages.LeaveGameRequest request)
    {
        throw new NotImplementedException();
    }

    private void JoinGame(CantStopMessages.JoinGameRequest request)
    {
        // is the player already in the game?
        foreach (var p in _players)
        {
            if (p.Id == request.Player.Id)
            {
                _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
                    request.Player,
                    CantStopOutgoing.VALIDATION_WARNING,
                    new ValidationWarning(Resources.PLAYER_ALREADY_IN_GAME)));
                return;
            }
        }

        if (_players.Count < _maxPlayers)
        {
            _players.Add(request.Player);
        }

        var data = GetPublicVisibleData();
        foreach (var p in _players)
        {
            if (p.Id == request.Player.Id)
            {
                _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
                    p,
                    CantStopOutgoing.SET_PLAYER_GAME,
                    data
                ));

                continue;
            }

            _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
                p,
                CantStopOutgoing.UPDATE_PUBLIC_VISIBLE,
                data
            ));
        }
    }

    private void StartGame(CantStopMessages.StartGameRequest request)
    {
        if (_owner == null) return;

        if (_owner.Id != request.Player.Id)
        {
            _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
                request.Player,
                CantStopOutgoing.VALIDATION_WARNING,
                new ValidationWarning(Resources.ONLY_OWNER_CAN_START)));
            return;
        }

        if (_players.Count < 2)
        {
            _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
                _owner,
                CantStopOutgoing.VALIDATION_WARNING,
                new ValidationWarning(Resources.NOT_ENOUGH_PLAYERS)));
            return;
        }

        // Start Game
    }

    private void SetupGame(CantStopMessages.SetupGameRequest request)
    {
        throw new NotImplementedException();
    }

    private void CreateGame(GameManagerMessages.CreateGameSpecificRequest request)
    {
        _owner = request.Player;

        _players = [_owner];

        _gameId = string.IsNullOrEmpty(request.GameId)
            ? _gameId
            : request.GameId;

        Context.Sender.Tell(new GameManagerNotifications.GameCreated(GetBaseDetails()));

        _hubWriter.Tell(new HubWriterMessages.WriteClientObject(
            _owner,
            CantStopOutgoing.SET_PLAYER_GAME,
            GetPublicVisibleData()));
    }

    private GameManagerNotifications.BaseGameNotification GetBaseDetails()
    {
        return _owner == null
            ? throw new InvalidGameStateException("No Owner detected")
            : new(_gameId, "cantstop", "cantstop", _gameStatus, _players.ToArray());
    }

    private PublicVisible GetPublicVisibleData() => new PublicVisible(_gameId, _players.ToArray(), _boardState);
}


