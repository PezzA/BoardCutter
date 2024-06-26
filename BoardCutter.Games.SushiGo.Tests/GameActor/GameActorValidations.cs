using Akka.Actor;
using Akka.TestKit.Xunit2;

using BoardCutter.Core.Actors.HubWriter;
using BoardCutter.Games.SushiGo.Actors.Game;
using BoardCutter.Games.SushiGo.Scoring;
using BoardCutter.Games.SushiGo.Shufflers;
using static BoardCutter.Core.Tests.TestDataSetup;

namespace BoardCutter.Games.SushiGo.Tests.GameActor;

[Trait("Category", "UnitTests")]
public class GameActorValidations : TestKit
{
    private readonly TimeSpan _noMsgTimeout = TimeSpan.FromMilliseconds(100);
    
    [Fact]
    public void GameActor_DoesNotExceedMaxPlayers()
    {
        var writerProbe = CreateTestProbe();

        var creatorPlayer = GetTestPlayer("creator");
        var guestOne = GetTestPlayer("guestOne");
        var guestTwo = GetTestPlayer("guestTwo");

        var gameId = "TestGameId";
        var gameActorProps = Props.Create(() =>
            new BoardCutter.Games.SushiGo.Actors.Game.GameActor(new RiggedCardShuffler(new List<Card>()), new Dictionary<CardType, IScorer>(),  writerProbe));

        var gameActor = Sys.ActorOf(gameActorProps, "gameActor");

        gameActor.Tell(new GameActorMessages.CreateGameRequest(creatorPlayer, gameId));

        var msg = ExpectMsg<GameActorMessages.GameCreated>();
        
        writerProbe.ExpectMsg<HubWriterActorMessages.WriteClientObject>();

        // Adding the first guest, should be fine
        gameActor.Tell(new GameActorMessages.JoinGameRequest(guestOne, gameId));

        ExpectMsg<GameActorMessages.GameUpdated>();
        // Both clients should get public gate state
        writerProbe.ExpectMsg<HubWriterActorMessages.WriteClientObject>();
        writerProbe.ExpectMsg<HubWriterActorMessages.WriteClientObject>();

        // Adding the second guest should trip the max player validation 
        gameActor.Tell(new GameActorMessages.JoinGameRequest(guestTwo, gameId));

        // Should write a message to "guestTwo" that max players has been reached
        var writeMsg = writerProbe.ExpectMsg<HubWriterActorMessages.WriteClientObject>();

        Assert.Contains("guestTwo", writeMsg.Player.ConnectionId);
        Assert.Equal(ServerMessages.ErrorMessage, writeMsg.Message);
        Assert.Equal(Resources.ResValidationMaxPlayers, writeMsg.Payload.ToString());
        
        writerProbe.ExpectNoMsg(_noMsgTimeout);
        ExpectNoMsg(_noMsgTimeout);
    }
}