# BoardCutter Load Test Client

A SignalR-based load testing client for the BoardCutter 2048 game server. This tool spawns multiple concurrent game clients to test server performance and scalability.

## Features

- Connects to BoardCutter Twenty48Hub via SignalR
- Spawns multiple concurrent game clients
- Plays games with random moves
- Tracks and reports performance statistics
- Automatic reconnection handling
- Thread-safe statistics collection

## Usage

### Basic Usage

```bash
# Run with defaults (1 client, 100 moves, localhost:5000)
dotnet run --project BoardCutter.LoadTest

# Run with custom parameters
dotnet run --project BoardCutter.LoadTest -- <serverUrl> <concurrentClients> <movesPerGame>
```

### Examples

```bash
# Single client, 100 moves (quick test)
dotnet run --project BoardCutter.LoadTest

# 10 concurrent clients, 50 moves each
dotnet run --project BoardCutter.LoadTest -- http://localhost:5000 10 50

# 100 concurrent clients, 200 moves each (stress test)
dotnet run --project BoardCutter.LoadTest -- http://localhost:5000 100 200

# Test against production server
dotnet run --project BoardCutter.LoadTest -- https://your-server.com 25 100
```

## Parameters

1. **serverUrl** (default: `http://localhost:5000`)
   - Base URL of the BoardCutter server
   - Do not include the hub path (`/twenty48hub`)

2. **concurrentClients** (default: `1`)
   - Number of simultaneous game clients to spawn
   - Each client creates its own game

3. **movesPerGame** (default: `100`)
   - Number of moves each client will make
   - Random directions (Up, Down, Left, Right)

## Output

The client provides real-time console output showing:
- Connection status for each client
- Game creation confirmations
- Game completion events with final scores
- Errors and disconnections

### Final Statistics

After all clients complete, you'll see:
- **Total Duration**: Time taken for all games
- **Games Created**: Number of successful game creations
- **Total Moves**: Total moves made across all clients
- **Failed Moves**: Moves that encountered errors
- **Disconnections**: Number of unexpected disconnections
- **Errors**: Total error count
- **Average Moves/sec**: Throughput metric
- **Success Rate**: Percentage of successful moves

## Example Output

```
BoardCutter 2048 Load Test Client
=================================

Server URL: http://localhost:5000
Concurrent Clients: 10
Moves per Game: 50

[Client 0] Connecting to http://localhost:5000/twenty48hub
[Client 1] Connecting to http://localhost:5000/twenty48hub
...
[Client 0] Connected successfully
[Client 0] Game created: abc-123-def
[Client 0] Starting to play game abc-123-def
...
[Client 0] Game over! Final score: 1024
[Client 0] Completed 50 moves

=================================
Load Test Results
=================================
Total Duration: 12.34s
Games Created: 10
Total Moves: 500
Failed Moves: 0
Disconnections: 0
Errors: 0
Average Moves/sec: 40.52
Success Rate: 100.00%
```

## Authentication

The client uses BoardCutter's anonymous authentication system:

1. **Pre-authentication**: Each client makes an HTTP GET to `/health` to trigger the `BoardCutterCookieMiddleware`
2. **Cookie Creation**: Server responds with a "BoardCutter" cookie containing a unique GUID
3. **SignalR Connection**: Client uses the same cookie for the SignalR hub connection
4. **Authorization**: The `BoardCutterAuthenticationHandler` validates the cookie and creates an anonymous user identity

Each client gets a unique anonymous identity like "Anonymous-abc12345".

No manual authentication setup is required - it's all handled automatically.

## Notes

- Moves are random, so games may end early if no valid moves exist
- Each client waits 50-150ms between moves to simulate realistic gameplay
- Clients automatically reconnect if disconnected
- The tool is single-run - it completes when all clients finish their moves

## Troubleshooting

**Connection refused errors:**
- Ensure the server is running
- Check the server URL is correct
- Verify CORS settings allow connections

**Authentication errors:**
- Check that `BoardCutterCookieMiddleware` is configured
- Verify the Twenty48Hub has proper authorization settings

**High error rates:**
- Reduce concurrent clients
- Increase delay between moves
- Check server logs for actor system errors
