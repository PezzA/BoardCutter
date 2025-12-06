<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import StatusBar from "./StatusBar.svelte";
  import GameCard from "./GameCard.svelte";
  import {
    createReactiveSignalRConnection,
    type ConnectionStatus,
  } from "./signalr.js";

  // Game list state
  let games = $state<any[]>([]);
  let loading = $state(true);
  let error = $state<string | null>(null);
  let connectionStatus = $state<ConnectionStatus>("disconnected");

  // Create SignalR connection
  const signalRConnection = createReactiveSignalRConnection({
    hubUrl: "/gamelobbyhub",
    withCredentials: true,
    automaticReconnect: true,
  });

  // Subscribe to status changes to update our reactive state
  signalRConnection.onStatusChange((status) => {
    connectionStatus = status;
  });

  async function connectToGameLobbyHub(): Promise<void> {
    try {
      // Set up event handlers before connecting
      signalRConnection.on("GameListUpdated", (gameList: any[] | string) => {
        console.log("Received game list update:", gameList);
        // Handle both array (direct call) and string (broadcast) formats
        if (typeof gameList === "string") {
          try {
            games = JSON.parse(gameList) || [];
          } catch (err) {
            console.error("Failed to parse game list:", err);
            games = [];
          }
        } else {
          games = gameList || [];
        }
        loading = false;
      });

      // Connect to the hub
      await signalRConnection.connect();

      // Join the GameLobby group to receive real-time updates
      await signalRConnection.invoke("JoinLobby");
      console.log("Joined GameLobby group for real-time updates");

      await requestGameList();
    } catch (err) {
      error = `Failed to connect to GameLobbyHub: ${err}`;
      console.error("GameLobbyHub connection error:", err);
      loading = false;
    }
  }

  async function requestGameList(): Promise<void> {
    if (signalRConnection.connection && connectionStatus === "connected") {
      try {
        loading = true;
        await signalRConnection.invoke("SendGameList");
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
    if (signalRConnection.connection && connectionStatus === "connected") {
      try {
        await signalRConnection.invoke("JoinGame", gameId);
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
    signalRConnection.disconnect();
    console.log("GameLobbyHub connection stopped");
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
        <GameCard {game} {connectionStatus} onJoinGame={joinGame} />
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
</style>
