﻿using BoardCutter.Core.Players;

namespace BoardCutter.Core.Actors;

public class GameManagerNotifications
{
    public record BaseGameNotification(string Id, string Title, string Tag, GameStatus Status, Player[] Players);

    public record GameCreated(BaseGameNotification Details);

    public record GameUpdated(BaseGameNotification Details);

    public record GameEnded(BaseGameNotification Details);
}