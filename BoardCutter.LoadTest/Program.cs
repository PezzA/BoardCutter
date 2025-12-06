using System.Diagnostics;
using System.Text.Json;
using BoardCutter.Games.Twenty48;
using Microsoft.AspNetCore.SignalR.Client;

namespace BoardCutter.LoadTest;

public class Program
{
    private static readonly Random Random = new();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("BoardCutter 2048 Load Test Client");
        Console.WriteLine("=================================\n");

        // Parse command line arguments
        var serverUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        var concurrentClients = args.Length > 1 ? int.Parse(args[1]) : 1;
        var movesPerGame = args.Length > 2 ? int.Parse(args[2]) : 100;
        var staggerDelayMs = args.Length > 3 ? int.Parse(args[3]) : 0;

        Console.WriteLine($"Server URL: {serverUrl}");
        Console.WriteLine($"Concurrent Clients: {concurrentClients}");
        Console.WriteLine($"Moves per Game: {movesPerGame}");
        Console.WriteLine($"Stagger Delay: {staggerDelayMs}ms");
        Console.WriteLine();

        var stats = new LoadTestStats();
        var stopwatch = Stopwatch.StartNew();

        // Create and run multiple concurrent game clients with optional stagger
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentClients; i++)
        {
            tasks.Add(RunGameClient(i, serverUrl, movesPerGame, stats));
            if (staggerDelayMs > 0 && i < concurrentClients - 1)
            {
                await Task.Delay(staggerDelayMs);
            }
        }

        await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Print statistics
        Console.WriteLine("\n=================================");
        Console.WriteLine("Load Test Results");
        Console.WriteLine("=================================");
        Console.WriteLine($"Total Duration: {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Games Created: {stats.GamesCreated}");
        Console.WriteLine($"Total Moves: {stats.TotalMoves}");
        Console.WriteLine($"Failed Moves: {stats.FailedMoves}");
        Console.WriteLine($"Disconnections: {stats.Disconnections}");
        Console.WriteLine($"Errors: {stats.Errors}");
        Console.WriteLine($"Average Moves/sec: {stats.TotalMoves / stopwatch.Elapsed.TotalSeconds:F2}");
        Console.WriteLine($"Success Rate: {(double)(stats.TotalMoves - stats.FailedMoves) / stats.TotalMoves * 100:F2}%");
    }

    private static async Task RunGameClient(int clientId, string serverUrl, int movesPerGame, LoadTestStats stats)
    {
        HubConnection? connection = null;
        string? gameId = null;
        var moveCount = 0;

        try
        {
            // Create a cookie container that will be shared between HTTP client and SignalR
            var cookieContainer = new System.Net.CookieContainer();

            // Make an initial HTTP request to get the BoardCutter authentication cookie
            using var httpClient = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer });
            try
            {
                // Request the health endpoint to trigger the cookie middleware
                var response = await httpClient.GetAsync($"{serverUrl}/health");
                Console.WriteLine($"[Client {clientId}] Pre-auth request status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client {clientId}] Pre-auth request failed: {ex.Message} (continuing anyway)");
            }

            Console.WriteLine($"[Client {clientId}] Connecting to {serverUrl}/twenty48hub");

            // Build the SignalR connection with the same cookie container
            connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/twenty48hub", options =>
                {
                    // Use the cookie container that already has the BoardCutter cookie
                    options.Cookies = cookieContainer;
                    options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                    {
                        CookieContainer = cookieContainer
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // Handle server messages
            connection.On<string>("SetPlayerGame", (data) =>
            {
                try
                {
                    var gameData = JsonSerializer.Deserialize<JsonElement>(data);
                    gameId = gameData.GetProperty("GameId").GetString(); // PascalCase from Newtonsoft.Json
                    Console.WriteLine($"[Client {clientId}] Game created: {gameId}");
                    Interlocked.Increment(ref stats.GamesCreated);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client {clientId}] Error parsing SetPlayerGame: {ex.Message}");
                }
            });

            connection.On<string>("PublicVisible", (data) =>
            {
                try
                {
                    var gameData = JsonSerializer.Deserialize<JsonElement>(data);
                    var score = gameData.GetProperty("Score").GetInt32(); // PascalCase
                    var status = gameData.GetProperty("Status").GetInt32(); // PascalCase

                    // GameStatus: 0=SettingUp, 1=Running, 2=Complete
                    if (status == 2) // Complete
                    {
                        Console.WriteLine($"[Client {clientId}] Game over! Final score: {score}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client {clientId}] Error parsing PublicVisible: {ex.Message}");
                }
            });

            connection.On<string>("ErrorMessage", (error) =>
            {
                Console.WriteLine($"[Client {clientId}] Server error: {error}");
                Interlocked.Increment(ref stats.Errors);
            });

            connection.On<string>("PlayerStatus", (status) =>
            {
                Console.WriteLine($"[Client {clientId}] Player status: {status}");
            });

            connection.Closed += async (error) =>
            {
                Console.WriteLine($"[Client {clientId}] Connection closed: {error?.Message ?? "Unknown"}");
                Interlocked.Increment(ref stats.Disconnections);
                await Task.CompletedTask;
            };

            // Connect to the hub
            await connection.StartAsync();
            Console.WriteLine($"[Client {clientId}] Connected successfully");

            // Wait a bit for connection to stabilize
            await Task.Delay(500);

            // Start a new game
            await connection.InvokeAsync("StartNew");
            Console.WriteLine($"[Client {clientId}] StartNew() called");

            // Wait for game to be created
            var waitCount = 0;
            while (gameId == null && waitCount < 20)
            {
                await Task.Delay(100);
                waitCount++;
            }

            if (gameId == null)
            {
                Console.WriteLine($"[Client {clientId}] Failed to create game (timeout)");
                return;
            }

            Console.WriteLine($"[Client {clientId}] Starting to play game {gameId}");

            // Play the game with random moves
            for (moveCount = 0; moveCount < movesPerGame; moveCount++)
            {
                var direction = (Direction)Random.Next(0, 4);

                try
                {
                    await connection.InvokeAsync("Move", gameId, direction);
                    Interlocked.Increment(ref stats.TotalMoves);

                    // Small delay between moves to simulate realistic gameplay
                    await Task.Delay(Random.Next(50, 150));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client {clientId}] Move failed: {ex.Message}");
                    Interlocked.Increment(ref stats.FailedMoves);
                }
            }

            Console.WriteLine($"[Client {clientId}] Completed {moveCount} moves");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client {clientId}] Error: {ex.Message}");
            Interlocked.Increment(ref stats.Errors);
        }
        finally
        {
            if (connection != null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private class LoadTestStats
    {
        public int GamesCreated;
        public int TotalMoves;
        public int FailedMoves;
        public int Disconnections;
        public int Errors;
    }
}
