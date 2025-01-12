using Akka.Actor;
using Akka.TestKit.Xunit2;

using BoardCutter.Core;
using BoardCutter.Core.Actors;

using static BoardCutter.Core.Tests.TestDataSetup;

namespace BoardCutter.Games.Twenty48.Tests;

[Trait("Category", "UnitTests")]
public class GameActorValidations : TestKit
{
    private readonly TimeSpan _noMsgTimeout = TimeSpan.FromMilliseconds(20);


    [Fact]
    public async void GameActor_CanStartGame()
    {
        var writerProbe = CreateTestProbe();
        var creatorPlayer = GetTestPlayer("creator");

        var gameId = "TestGameId";

        Props gameActorProps = Props.Create(() => new GameActor(writerProbe, new PredictableTilePlacer()));

        var gameActor = ActorOfAsTestActorRef<GameActor>(gameActorProps, TestActor);

        // Game Manager tells actor to Create the game.  This should result in a running game.
        var resp = await gameActor.Ask(new GameManagerMessages.CreateGameSpecificRequest(creatorPlayer, gameId), _noMsgTimeout) as GameManagerNotifications.GameCreated;

        Assert.NotNull(resp);
        Assert.Equal(gameId, resp.Details.Id);
        Assert.Equal(GameStatus.Running, resp.Details.Status );
         
        // Make sure the client gets the game message.
        var msg = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg.Payload);

        var unwrappedMsg = msg.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg);
        Assert.Equal(GameStatus.Running, unwrappedMsg.Status);
        Assert.Equal(gameId, unwrappedMsg.GameId);
        Assert.Equal(0, unwrappedMsg.Score);
     
        // Make a move
        gameActor.Tell(new GameMessages.MoveRequest(creatorPlayer, Direction.Right));
        var msg2 = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg2.Payload);

        var unwrappedMsg2 = msg2.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg2);
        Assert.Equal(gameId, unwrappedMsg2.GameId);
        //Assert.Equal(4, unwrappedMsg2.Score);

        // Make another move
        gameActor.Tell(new GameMessages.MoveRequest(creatorPlayer, Direction.Down));

        var msg3 = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg3.Payload);

        var unwrappedMsg3 = msg3.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg3);
        Assert.Equal(gameId, unwrappedMsg3.GameId);
        //Assert.Equal(4, unwrappedMsg3.Score);
        

        // Finish
        await writerProbe.ExpectNoMsgAsync(_noMsgTimeout);
        await ExpectNoMsgAsync(_noMsgTimeout);
    }
}