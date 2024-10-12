using Akka.Actor;

using BoardCutter.Core;
using BoardCutter.Core.Actors;
using BoardCutter.Core.Exceptions;
using BoardCutter.Core.Players;

namespace BoardCutter.Games.Twenty48;



public class GameActor : ReceiveActor
{
    private Player? _owner;
    private string _gameId = Guid.NewGuid().ToString();
    private int _gridSize = 4;
    private Dictionary<int, NumberCell> _cells = [];
    private int _score;
    private readonly IActorRef _hubWriterActor;
    private readonly ITilePlacer _tilePlacer;
    private GameStatus _gameStatus = GameStatus.SettingUp;

    private static int GetNextId(Dictionary<int, NumberCell> grid)
    {
        int max = 0;
        foreach ((int key, _) in grid)
        {
            if (key > max)
            {
                max = key;
            }
        }

        return max + 1;
    }

    private GameManagerNotifications.BaseGameNotification GetBaseDetails()
    {
        if (_owner == null)
        {
            throw new InvalidGameStateException("No Owner detected");
        }

        return new(_gameId, "2048", "2048", _gameStatus, [_owner]);
    }

    public GameActor(IActorRef hubWriterActor, ITilePlacer tilePlacer)
    {
        _hubWriterActor = hubWriterActor;
        _tilePlacer = tilePlacer;

        // 'Generic' Messages
        Receive<GameManagerMessages.CreateGameSpecificRequest>(CreateGame);

        // Game Specific Messages
        Receive<GameMessages.SetupGameRequest>(SetupRequest);
        Receive<GameMessages.StartGameRequest>(StartGameRequest);
        Receive<GameMessages.LeaveGameRequest>(LeaveGameRequest);
        Receive<GameMessages.MoveRequest>(MoveRequest);
        Receive<GameMessages.BroadcastRequest>(BroadcastRequest);
    }

    private void BroadcastRequest(GameMessages.BroadcastRequest msg) => BroadCastVisible();

    private void LeaveGameRequest(GameMessages.LeaveGameRequest message)
    {

        if (_owner == null)
        {
            throw new InvalidGameStateException("Owner is null");
        }

        if (message.Player.Id != _owner.Id)
        {
            _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(message.Player, "Error",
                "Only the game creator can setup game properties"));
            return;
        }

        Context.Parent.Tell(new GameManagerNotifications.GameEnded(GetBaseDetails()));
    }

    private void SetupRequest(GameMessages.SetupGameRequest message)
    {
        if (_owner == null)
        {
            throw new InvalidGameStateException("Owner is null");
        }

        if (message.Player.Id != _owner.Id)
        {
            _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(message.Player, "Error",
                "Only the game creator can setup game properties"));
            return;
        }

        _gridSize = message.GridSize;

        BroadCastVisible();
    }

    private void StartGameRequest(GameMessages.StartGameRequest message)
    {
        _cells = new Dictionary<int, NumberCell>();

        (Point2D p1, int val1) = _tilePlacer.PlaceTile(_cells, _gridSize);
        var cell1 = new NumberCell(GetNextId(_cells), val1, p1, true, false);
        _cells[cell1.Id] = cell1;

        (Point2D p2, int val2) = _tilePlacer.PlaceTile(_cells, _gridSize);
        var cell2 = new NumberCell(GetNextId(_cells), val2, p2, true, false);
        _cells[cell2.Id] = cell2;

        _score = 0;

        SetGameStatus(GameStatus.Running);
        BroadCastVisible();
    }

    private void CreateGame(GameManagerMessages.CreateGameSpecificRequest message)
    {
        _owner = message.Player;
        _gameId = string.IsNullOrEmpty(message.GameId)
            ? _gameId
            : message.GameId;

        Context.Parent.Tell(new GameManagerNotifications.GameCreated(GetBaseDetails()));

        _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(
            _owner,
            "SetPlayerGame",
            GetPublicVisibleData()));

        StartGameRequest(new GameMessages.StartGameRequest(_owner));
    }

    private void MoveRequest(GameMessages.MoveRequest message)
    {
        if (_owner == null)
        {
            throw new InvalidGameStateException("Owner is null");
        }

        if (message.Player.Id != _owner.Id)
        {
            _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(
                _owner,
                Server2048Messages.ErrorMessage,
                "Only the game oweer can make a move"));
            return;
        }

        if (_gameStatus == GameStatus.Complete)
        {
            _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(
                _owner,
                Server2048Messages.ErrorMessage,
                "Game is complete and no more moves can be played"));
            return;
        }

        // if we are accepting a move, we have to remove any destroyed cell

        var cellList = _cells.Where(c => c.Value.Destroy).ToList();

        foreach (var cell in cellList)
        {
            _cells.Remove(cell.Key);
        }

        foreach (var cell in _cells.Where(cell => cell.Value.New))
        {
            _cells[cell.Key] = _cells[cell.Key] with { New = false };
        }

        (bool hasChange, int scoreIncrement) = message.Direction switch
        {
            Direction.Up => ShuntUp(_cells, _gridSize),
            Direction.Down => ShuntDown(_cells, _gridSize),
            Direction.Left => ShuntLeft(_cells, _gridSize),
            Direction.Right => ShuntRight(_cells, _gridSize),
            _ => throw new ArgumentOutOfRangeException(nameof(message))
        };

        _score += scoreIncrement;

        // only place the next tile if there has been a change in the grid.
        if (hasChange)
        {
            (Point2D point, int val1) = _tilePlacer.PlaceTile(_cells, _gridSize);
            var nextId = GetNextId(_cells);
            _cells[nextId] = new NumberCell(nextId, val1, point, true, false);
        }

        if (IsGameOver(_cells, _gridSize))
        {
            SetGameStatus(GameStatus.Complete);
        }

        BroadCastVisible();
    }

    private void SetGameStatus(GameStatus status)
    {
        _gameStatus = status;

        Context.Parent.Tell(new GameManagerNotifications.GameUpdated(GetBaseDetails()));
    }


    private static bool CompareCell(Dictionary<int, NumberCell> grid, Point2D testPoint, NumberCell cell)
    {
        var testCellPoint = cell.Point.Add(testPoint);

        var testCell = GridExtensions.GetByPos(testCellPoint, grid);

        if (testCell != null)
        {
            if (testCell.Value == cell.Value && !testCell.Destroy)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGameOver(Dictionary<int, NumberCell> grid, int gridSize)
    {
        var jaggedArray = GridExtensions.ToJaggedArray(grid, gridSize);

        for (int y = 0; y < jaggedArray.Length; y++)
        {
            for (int x = 0; x < jaggedArray[y].Length; x++)
            {
                var cell = jaggedArray[y][x];
                if (cell == 0)
                {
                    return false;
                }

                // Are there any adjacent cells of the same value?

                // up
                if (y > 0 && jaggedArray[y - 1][x] == cell)
                {
                    return false;
                }

                // down
                if (y < jaggedArray.Length - 1 && jaggedArray[y + 1][x] == cell)
                {
                    return false;
                }

                // left
                if (x > 0 && jaggedArray[y][x - 1] == cell)
                {
                    return false;
                }

                // right
                if (x < jaggedArray[y].Length - 1 && jaggedArray[y][x + 1] == cell)
                {
                    return false;
                }
            }
        }

        return true;
    }


    public static (bool hasChanged, int increment) ShuntUp(Dictionary<int, NumberCell> grid, int gridSize)
    {
        var hasShuntChange = false;
        var shuntIncrement = 0;

        for (int col = 0; col < gridSize; col++)
        {
            var col1 = col;
            var shuntIds = grid.Where(c => c.Value.Point.X == col1).OrderBy(c => c.Value.Point.Y)
                .Select(c => c.Value.Id);

            foreach (var id in shuntIds)
            {
                (bool hasChange, int increment) = ShuntCell(id, grid, 0, -1, gridSize);

                shuntIncrement += increment;
                if (hasChange)
                {
                    hasShuntChange = true;
                }
            }
        }

        return (hasShuntChange, shuntIncrement);
    }

    public static (bool hasChanged, int increment) ShuntDown(Dictionary<int, NumberCell> grid, int gridSize)
    {
        var hasShuntChange = false;
        var shuntIncrement = 0;

        for (int col = 0; col < gridSize; col++)
        {
            var col1 = col;
            var shuntIds = grid.Where(c => c.Value.Point.X == col1).OrderByDescending(c => c.Value.Point.Y)
                .Select(c => c.Value.Id);

            foreach (var id in shuntIds)
            {
                (bool hasChange, int increment) = ShuntCell(id, grid, 0, 1, gridSize);

                shuntIncrement += increment;
                if (hasChange)
                {
                    hasShuntChange = true;
                }
            }
        }

        return (hasShuntChange, shuntIncrement);
    }

    public static (bool hasChanged, int increment) ShuntRight(Dictionary<int, NumberCell> grid, int gridSize)
    {
        var hasShuntChange = false;
        var shuntIncrement = 0;

        for (int row = 0; row < gridSize; row++)
        {
            var row1 = row;
            var shuntIds = grid.Where(c => c.Value.Point.Y == row1).OrderByDescending(c => c.Value.Point.X)
                .Select(c => c.Value.Id);

            foreach (var id in shuntIds)
            {
                (bool hasChange, int increment) = ShuntCell(id, grid, 1, 0, gridSize);

                shuntIncrement += increment;
                if (hasChange)
                {
                    hasShuntChange = true;
                }
            }
        }

        return (hasShuntChange, shuntIncrement);
    }


    public static (bool hasChanged, int increment) ShuntLeft(Dictionary<int, NumberCell> grid, int gridSize)
    {
        var hasShuntChange = false;
        var shuntIncrement = 0;

        for (int row = 0; row < gridSize; row++)
        {
            var row1 = row;
            var shuntIds = grid.Where(c => c.Value.Point.Y == row1).OrderBy(c => c.Value.Point.X)
                .Select(c => c.Value.Id);

            foreach (var id in shuntIds)
            {
                (bool hasChange, int increment) = ShuntCell(id, grid, -1, 0, gridSize);

                shuntIncrement += increment;
                if (hasChange)
                {
                    hasShuntChange = true;
                }
            }
        }

        return (hasShuntChange, shuntIncrement);
    }


    public static (bool hasChanged, int increment) ShuntCell(
        int cellId,
        Dictionary<int, NumberCell> grid,
        int xInc,
        int yInc,
        int gridSize)
    {
        bool hasChanged = false;
        int scoreIncrement = 0;

        var cell = GridExtensions.GetById(cellId, grid);

        if (cell == null) throw new InvalidGameStateException($"Failed to find cell {cellId}");

        while (true)
        {
            NumberCell testCell = cell with { Point = new Point2D(cell.Point.X + xInc, cell.Point.Y + yInc) };


            if (testCell.Point.X == gridSize || testCell.Point.Y == gridSize || testCell.Point.X < 0 ||
                testCell.Point.Y < 0) break;

            var targetCell = GridExtensions.GetByPos(testCell.Point, grid);

            if (targetCell == null)
            {
                cell = testCell;
                grid[cell.Id] = cell;
                hasChanged = true;
                continue;
            }

            if (targetCell.Value == cell.Value && !targetCell.New)
            {
                grid[targetCell.Id] = grid[targetCell.Id] with { Point = testCell.Point, Destroy = true };
                grid[cell.Id] = grid[cell.Id] with { Point = testCell.Point, Destroy = true };

                int nextId = GetNextId(grid);

                grid[nextId] = new NumberCell(nextId, targetCell.Value * 2, testCell.Point, true, false);
                return (true, targetCell.Value * 2);
            }

            return (hasChanged, scoreIncrement);
        }

        return (hasChanged, scoreIncrement);
    }

    private PublicVisible GetPublicVisibleData() =>
        new(_gameId, _score, _gameStatus, _gridSize, _cells.Select(kv => kv.Value).ToArray());

    private void BroadCastVisible()
    {
        if (_owner == null)
        {
            throw new InvalidGameStateException("Game Owner is null");
        }

        _hubWriterActor.Tell(new HubWriterMessages.WriteClientObject(_owner,
            Server2048Messages.PublicVisible,
            GetPublicVisibleData()));
    }
}