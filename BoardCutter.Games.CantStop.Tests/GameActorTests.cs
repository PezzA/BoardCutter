using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;

using BoardCutter.Core;
using BoardCutter.Core.Actors;
using BoardCutter.Core.Players;

using static BoardCutter.Core.Tests.TestDataSetup;

namespace BoardCutter.Games.CantStop.Tests;

public class GameActorTests : TestKit
{
    // higher than it needs to be, but build agents are adverse to throttling
    private readonly TimeSpan _noMsgTimeout = TimeSpan.FromMilliseconds(100);

    private readonly TestProbe _writerProbe;

    private readonly string _gameId;

    private readonly Player _owner;

    private const string TEST_GAME_ID = "TestGameId";

    private const string TEST_GAME_OWNER = "TestOwner";

    public GameActorTests()
    {
        _writerProbe = CreateTestProbe();
        _gameId = TEST_GAME_ID;
        _owner = GetTestPlayer(TEST_GAME_OWNER);
    }

    [Fact]
    public async Task GameActor_01_Parent_Can_Create_Game()
    {
        var thrower = new ProgrammableDiceThrower([]);

        Props gameActorProps = Props.Create(() => new GameActor(_writerProbe, thrower));
        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        var response = await gameActor.Ask(
            new GameManagerMessages.CreateGameSpecificRequest(_owner, _gameId),
            _noMsgTimeout) as GameManagerNotifications.GameCreated;

        Assert.NotNull(response);
        Assert.Equal(_gameId, response.Details.Id);
        Assert.Equal(_owner.Name, response.Details.Players[0].Name);
        Assert.Equal(GameStatus.SettingUp, response.Details.Status);
    }

    [Fact]
    public async Task GameActor_02_Cant_Start_Game_With_One_Player()
    {
        var thrower = new ProgrammableDiceThrower([]);

        Props gameActorProps = Props.Create(() => new GameActor(_writerProbe, thrower));
        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        var response = await gameActor.Ask(
            new GameManagerMessages.CreateGameSpecificRequest(_owner, _gameId),
            _noMsgTimeout) as GameManagerNotifications.GameCreated;

        gameActor.Tell(new CantStopMessages.StartGameRequest(_owner));

        var validationMessage = _writerProbe.FishForMessage<HubWriterMessages.WriteClientObject>(
            m => m.Player.Id == _owner.Id && m.Message == CantStopOutgoing.VALIDATION_WARNING, _noMsgTimeout);

        var validatePayload = validationMessage.Payload as ValidationWarning;

        Assert.NotNull(validatePayload);
        Assert.Equal(Resources.NOT_ENOUGH_PLAYERS, validatePayload.Message);
    }

    [Fact]
    public async Task GameActor_03_Only_The_Game_Owner_Can_Start()
    {
        var thrower = new ProgrammableDiceThrower([]);

        Props gameActorProps = Props.Create(() => new GameActor(_writerProbe, thrower));
        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        var response = await gameActor.Ask(
            new GameManagerMessages.CreateGameSpecificRequest(_owner, _gameId),
            _noMsgTimeout) as GameManagerNotifications.GameCreated;

        var otherPlayer = GetTestPlayer("OtherPlayer");

        gameActor.Tell(new CantStopMessages.StartGameRequest(otherPlayer));

        var validationMessage = _writerProbe.FishForMessage<HubWriterMessages.WriteClientObject>(
            m => m.Player.Id == otherPlayer.Id && m.Message == CantStopOutgoing.VALIDATION_WARNING, _noMsgTimeout);

        var validatePayload = validationMessage.Payload as ValidationWarning;

        Assert.NotNull(validatePayload);
        Assert.Equal(Resources.ONLY_OWNER_CAN_START, validatePayload.Message);
    }

    [Fact]
    public async Task GameActor_04_Player_Can_Join_Game()
    {
        var thrower = new ProgrammableDiceThrower([]);

        Props gameActorProps = Props.Create(() => new GameActor(_writerProbe, thrower));
        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        var response = await gameActor.Ask(
            new GameManagerMessages.CreateGameSpecificRequest(_owner, _gameId),
            _noMsgTimeout) as GameManagerNotifications.GameCreated;

        var player2 = GetTestPlayer("Player2");

        gameActor.Tell(new CantStopMessages.JoinGameRequest(player2));

        var joinMessage = _writerProbe.FishForMessage<HubWriterMessages.WriteClientObject>(
            msg => msg.Player.Name == player2.Name && msg.Message == CantStopOutgoing.SET_PLAYER_GAME,
            _noMsgTimeout);
    }

    [Fact]
    public async Task GameActor_05_Player_Cannot_Join_Twice()
    {
        var thrower = new ProgrammableDiceThrower([]);

        Props gameActorProps = Props.Create(() => new GameActor(_writerProbe, thrower));
        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        var response = await gameActor.Ask(
            new GameManagerMessages.CreateGameSpecificRequest(_owner, _gameId),
            _noMsgTimeout) as GameManagerNotifications.GameCreated;

        gameActor.Tell(new CantStopMessages.JoinGameRequest(_owner));

        var validationMessage = _writerProbe.FishForMessage<HubWriterMessages.WriteClientObject>(
                msg => msg.Player.Id == _owner.Id && msg.Message == CantStopOutgoing.VALIDATION_WARNING,
                _noMsgTimeout);

        var validationMessagePayload = validationMessage.Payload as ValidationWarning;

        Assert.NotNull(validationMessagePayload);
        Assert.Equal(Resources.PLAYER_ALREADY_IN_GAME, validationMessagePayload.Message);
    }
}
