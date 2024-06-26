﻿using BoardCutter.Core.Players;

namespace BoardCutter.Games.SushiGo.Models;

public class PublicVisible
{
    public string GameTag => "SushiGo";
    public string Id { get; set; } = string.Empty;

    public GameStatus Status{ get; set; }

    public GameParameters Parameters { get; set; } = null!;

    public List<Player> Players { get; set; } = [];
}