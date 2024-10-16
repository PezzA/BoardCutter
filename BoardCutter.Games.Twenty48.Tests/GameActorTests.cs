﻿using Akka.Actor;
using Akka.TestKit.Xunit2;

using BoardCutter.Core;
using BoardCutter.Core.Actors;

using static BoardCutter.Core.Tests.TestDataSetup;

namespace BoardCutter.Games.Twenty48.Tests;

[Trait("Category", "UnitTests")]
public class GameActorValidations : TestKit
{
    private readonly TimeSpan _noMsgTimeout = TimeSpan.FromMilliseconds(100);


    [Fact(Skip = "Will need some udpates")]
    public async void GameActor_CanStartGame()
    {
        var writerProbe = CreateTestProbe();
        var creatorPlayer = GetTestPlayer("creator");

        var gameId = "TestGameId";

        var gameActorProps = Props.Create(
            () => new GameActor(writerProbe, new PredictableTilePlacer()));

        var gameActor = Sys.ActorOf(gameActorProps, "gameActor");

        var resp = await gameActor.Ask(new GameManagerMessages.CreateGameSpecificRequest(creatorPlayer, gameId), _noMsgTimeout) as GameManagerNotifications.GameUpdated;

        Assert.NotNull(resp);
        Assert.Equal(gameId, resp.Details.Id);
        Assert.Equal(GameStatus.SettingUp, resp.Details.Status );
        
        // Game Setup
        gameActor.Tell(new GameMessages.SetupGameRequest(creatorPlayer, 4));

        var setupMsg = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);
        
        Assert.IsType<PublicVisible>(setupMsg.Payload);

        var unwrappedSetupMsg = setupMsg.Payload as PublicVisible;

        Assert.NotNull(unwrappedSetupMsg);
        Assert.Equal(GameStatus.SettingUp, unwrappedSetupMsg.Status);
        Assert.Equal(gameId, unwrappedSetupMsg.GameId);
        Assert.Equal(0, unwrappedSetupMsg.Score);
      //  Assert.Equal([], unwrappedSetupMsg.Grid);
        
        // Start Game
        gameActor.Tell(new GameMessages.StartGameRequest(creatorPlayer));
        
        var msg = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg.Payload);

        var unwrappedMsg = msg.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg);
        Assert.Equal(GameStatus.Running, unwrappedMsg.Status);
        Assert.Equal(gameId, unwrappedMsg.GameId);
        Assert.Equal(0, unwrappedMsg.Score);
      //  Assert.Equal(new int[][] { [2, 2, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0] }, unwrappedMsg.Cells);
        
        var startNotification = ExpectMsg<GameManagerNotifications.GameUpdated>(_noMsgTimeout);
        Assert.Equal(GameStatus.Running, startNotification.Details.Status);
        
        // Make a move
        gameActor.Tell(new GameMessages.MoveRequest(creatorPlayer, Direction.Right));

        var msg2 = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg2.Payload);

        var unwrappedMsg2 = msg2.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg2);
        Assert.Equal(gameId, unwrappedMsg2.GameId);
        Assert.Equal(4, unwrappedMsg2.Score);
      //  Assert.Equal(new int[][] { [2, 0, 0, 4], [0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0] }, unwrappedMsg2.Grid);

        // Make another move
        gameActor.Tell(new GameMessages.MoveRequest(creatorPlayer, Direction.Down));

        var msg3 = writerProbe.ExpectMsg<HubWriterMessages.WriteClientObject>(_noMsgTimeout);

        Assert.IsType<PublicVisible>(msg3.Payload);

        var unwrappedMsg3 = msg3.Payload as PublicVisible;

        Assert.NotNull(unwrappedMsg3);
        Assert.Equal(gameId, unwrappedMsg3.GameId);
        Assert.Equal(4, unwrappedMsg3.Score);
        //Assert.Equal(new int[][] { [2, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0], [2, 0, 0, 4] }, unwrappedMsg3.Grid);

        // Finish
        await writerProbe.ExpectNoMsgAsync(_noMsgTimeout);
        await ExpectNoMsgAsync(_noMsgTimeout);
    }
}