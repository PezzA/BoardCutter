<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import * as signalR from "@microsoft/signalr";
  import StatusBar from "./StatusBar.svelte";

  // Game list state
  let games = $state<any[]>([]);
  let connection: signalR.HubConnection | null = $state(null);
  let connectionStatus = $state<
    "disconnected" | "reconnecting" | "connected" | "error"
  >("disconnected");
  let loading = $state(true);
  let error = $state<string | null>(null);

  async function connectToGameLobbyHub(): Promise<void> {
    try {
      connectionStatus = "reconnecting";

      connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamelobbyhub", {
          withCredentials: true,
        })
        .withAutomaticReconnect()
        .build();

      // Handle connection state changes
      connection.onclose(() => {
        connectionStatus = "disconnected";
        console.log("GameLobbyHub connection closed");
      });

      connection.onreconnecting(() => {
        connectionStatus = "reconnecting";
        console.log("GameLobbyHub reconnecting...");
      });

      connection.onreconnected(() => {
        connectionStatus = "connected";
        console.log("GameLobbyHub reconnected");
        requestGameList();
      });

      // Handle incoming game list updates
      connection.on("GameListUpdated", (gameList: any[]) => {
        console.log("Received game list update:", gameList);
        games = gameList || [];
        loading = false;
      });

      // Handle individual game updates
      connection.on("GameUpdate", (gameData: any) => {
        console.log("Received game update:", gameData);
        // Update existing game or add new one
        const gameIndex = games.findIndex((g) => g.id === gameData.id);
        if (gameIndex >= 0) {
          games[gameIndex] = gameData;
        } else {
          games = [...games, gameData];
        }
      });

      // Handle game removal
      connection.on("GameRemoved", (gameId: string) => {
        console.log("Game removed:", gameId);
        games = games.filter((g) => g.id !== gameId);
      });

      // Start the connection
      await connection.start();
      connectionStatus = "connected";
      console.log("Connected to GameLobbyHub");

      // Some hubs automatically send game list on connection, wait a moment
      setTimeout(async () => {
        if (games.length === 0) {
          // Request the initial game list if not received automatically
          await requestGameList();
        } else {
          loading = false;
        }
      }, 1000);
    } catch (err) {
      connectionStatus = "error";
      error = `Failed to connect to GameLobbyHub: ${err}`;
      console.error("GameLobbyHub connection error:", err);
      loading = false;
    }
  }

  async function requestGameList(): Promise<void> {
    if (connection && connectionStatus === "connected") {
      try {
        loading = true;
        await connection.invoke("SendGameList");
        console.log("Requested game list using SendGameList");
      } catch (err) {
        error = `Failed to request game list: ${err}`;
        console.error("Failed to invoke SendGameList:", err);
        loading = false;
      }
    }
  }

  async function refreshGameList(): Promise<void> {
    error = null;
    await requestGameList();
  }

  async function joinGame(gameId: string): Promise<void> {
    if (connection && connectionStatus === "connected") {
      try {
        await connection.invoke("JoinGame", gameId);
        console.log("Joined game:", gameId);
        // Navigate to game or handle join response
      } catch (err) {
        error = `Failed to join game: ${err}`;
        console.error("Failed to join game:", err);
      }
    }
  }

  onMount(() => {
    connectToGameLobbyHub();
  });

  onDestroy(() => {
    if (connection) {
      connection.stop();
      console.log("GameLobbyHub connection stopped");
    }
  });
</script>

<div class="game-list-container">
  <StatusBar {connectionStatus} />

  <div class="header">
    <h2>Game Lobby</h2>
  </div>

  {#if error}
    <div class="error-message">
      <strong>Error:</strong>
      {error}
      <button
        class="retry-button"
        onclick={() => {
          error = null;
          connectToGameLobbyHub();
        }}
      >
        Retry Connection
      </button>
    </div>
  {/if}

  {#if loading}
    <div class="loading">
      <div class="loading-spinner"></div>
      <p>Loading games...</p>
    </div>
  {:else if games.length === 0}
    <div class="no-games">
      <p>No active games found.</p>
    </div>
  {:else}
    <div class="games-grid">
      {#each games as game (game.id)}
        <div class="game-card">
          <div class="game-header">
            <h3 class="game-title">
              {game.title || `Game ${game.id}`}
            </h3>
            <span
              class="game-status"
              class:active={game.status === 1}
              class:waiting={game.status === 0}
              class:full={game.status === 2}
            >
              {game.status === 1
                ? "active"
                : game.status === 0
                  ? "waiting"
                  : game.status === 2
                    ? "full"
                    : "unknown"}
            </span>
          </div>

          <div class="game-info">
            <div class="info-row">
              <span class="label">Players:</span>
              <span class="value">{game.players?.length || 0}</span>
            </div>

            {#if game.players && game.players.length > 0}
              <div class="info-row">
                <span class="label">Host:</span>
                <span class="value">{game.players[0].name || "Anonymous"}</span>
              </div>
            {/if}
          </div>
          <div class="game-actions">
            <button
              class="join-button"
              onclick={() => joinGame(game.id)}
              disabled={connectionStatus !== "connected" || game.status === 2}
            >
              {game.status === 2 ? "Full" : "Join Game"}
            </button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .game-list-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1rem;
    font-family: Arial, sans-serif;
  }

  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    padding-bottom: 1rem;
    border-bottom: 2px solid #e0e0e0;
  }

  .header h2 {
    margin: 0;
    color: #333;
  }

  .error-message {
    background-color: #ffebee;
    border: 1px solid #f44336;
    border-radius: 5px;
    padding: 1rem;
    margin-bottom: 1rem;
    color: #c62828;
  }

  .retry-button {
    background-color: #f44336;
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 3px;
    cursor: pointer;
    margin-left: 1rem;
  }

  .loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 2rem;
  }

  .loading-spinner {
    width: 40px;
    height: 40px;
    border: 4px solid #f0f0f0;
    border-top: 4px solid #2196f3;
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }

  .no-games {
    text-align: center;
    padding: 2rem;
    color: #666;
  }

  .games-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1rem;
  }

  .game-card {
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 1rem;
    background-color: #fff;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition: box-shadow 0.3s ease;
  }

  .game-card:hover {
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  }

  .game-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
  }

  .game-title {
    margin: 0;
    font-size: 1.2rem;
    color: #333;
  }

  .game-status {
    padding: 0.25rem 0.75rem;
    border-radius: 15px;
    font-size: 0.8rem;
    font-weight: 500;
    text-transform: uppercase;
  }

  .game-status.active {
    background-color: #e8f5e8;
    color: #2e7d32;
  }

  .game-status.waiting {
    background-color: #fff3e0;
    color: #f57c00;
  }

  .game-status.full {
    background-color: #ffebee;
    color: #c62828;
  }

  .game-info {
    margin-bottom: 1rem;
  }

  .info-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.5rem;
  }

  .label {
    font-weight: 500;
    color: #666;
  }

  .value {
    color: #333;
  }

  .game-actions {
    display: flex;
    gap: 0.5rem;
  }

  .join-button {
    flex: 1;
    background-color: #4caf50;
    color: white;
    border: none;
    padding: 0.75rem;
    border-radius: 5px;
    cursor: pointer;
    font-size: 1rem;
    transition: background-color 0.3s ease;
  }

  .join-button:hover:not(:disabled) {
    background-color: #45a049;
  }

  .join-button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
  }
</style>
