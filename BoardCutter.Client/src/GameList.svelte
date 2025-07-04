<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';

  interface GameData {
    id: string;
    title: string;
    tag: string;
    status: number;
    players: Player[];
  }

  interface Player {
    name: string;
    connectionId: string;
    isOnline: boolean;
  }

  let connection: HubConnection | null = null;
  let games: GameData[] = [];
  let isConnected = false;
  let error: string | null = null;

  async function connectToLobby() {
    try {
      connection = new HubConnectionBuilder()
        .withUrl("http://localhost:5000/gamelobbyhub", {
          withCredentials: true
        })
        .build();

      connection.on("GameListUpdated", (gameList: GameData[]) => {
        games = gameList;
        console.log("Game list updated:", gameList);
      });

      connection.on("Error", (errorMessage: string) => {
        error = errorMessage;
        console.error("Hub error:", errorMessage);
      });

      connection.onclose(() => {
        isConnected = false;
        console.log("Disconnected from game lobby");
      });

      await connection.start();
      isConnected = true;
      console.log("Connected to game lobby");
      
      // Join the lobby and get initial game list
      await connection.invoke("JoinLobby");
    } catch (err) {
      error = `Failed to connect: ${err}`;
      console.error("Connection error:", err);
    }
  }

  async function createGame(gameTag: string) {
    if (connection && isConnected) {
      try {
        await connection.invoke("CreateGame", gameTag);
      } catch (err) {
        error = `Failed to create game: ${err}`;
        console.error("Create game error:", err);
      }
    }
  }

  async function refreshGameList() {
    if (connection && isConnected) {
      try {
        await connection.invoke("GetGameList");
      } catch (err) {
        error = `Failed to refresh game list: ${err}`;
        console.error("Refresh error:", err);
      }
    }
  }

  function getStatusText(status: number): string {
    switch (status) {
      case 0: return "Waiting";
      case 1: return "In Progress";
      case 2: return "Completed";
      default: return "Unknown";
    }
  }

  function getStatusClass(status: number): string {
    switch (status) {
      case 0: return "status-waiting";
      case 1: return "status-playing";
      case 2: return "status-completed";
      default: return "status-unknown";
    }
  }

  onMount(() => {
    connectToLobby();
  });

  onDestroy(() => {
    if (connection) {
      connection.stop();
    }
  });
</script>

<div class="game-lobby">
  <h2>Game Lobby</h2>
  
  {#if error}
    <div class="alert alert-danger">
      {error}
      <button class="btn-close" on:click={() => error = null}>&times;</button>
    </div>
  {/if}

  <div class="lobby-controls">
    <button 
      class="btn btn-primary" 
      on:click={() => createGame("2048")}
      disabled={!isConnected}
    >
      Create New 2048 Game
    </button>
    
    <button 
      class="btn btn-secondary" 
      on:click={refreshGameList}
      disabled={!isConnected}
    >
      Refresh
    </button>
    
    <span class="connection-status {isConnected ? 'connected' : 'disconnected'}">
      {isConnected ? 'Connected' : 'Disconnected'}
    </span>
  </div>

  <div class="games-container">
    {#if games.length === 0}
      <div class="no-games">
        <p>No games available. Create a new game to get started!</p>
      </div>
    {:else}
      <div class="games-grid">
        {#each games as game (game.id)}
          <div class="game-card">
            <div class="game-header">
              <h3>{game.title || `${game.tag} Game`}</h3>
              <span class="game-status {getStatusClass(game.status)}">
                {getStatusText(game.status)}
              </span>
            </div>
            
            <div class="game-details">
              <p><strong>Game ID:</strong> {game.id}</p>
              <p><strong>Type:</strong> {game.tag}</p>
              <p><strong>Players:</strong> {game.players.length}</p>
            </div>
            
            {#if game.players.length > 0}
              <div class="players-list">
                <h4>Players:</h4>
                <ul>
                  {#each game.players as player}
                    <li class="player {player.isOnline ? 'online' : 'offline'}">
                      {player.name}
                      <span class="player-status">
                        {player.isOnline ? '🟢' : '🔴'}
                      </span>
                    </li>
                  {/each}
                </ul>
              </div>
            {/if}
            
            <div class="game-actions">
              {#if game.status === 0}
                <button class="btn btn-success btn-sm">
                  Join Game
                </button>
              {:else if game.status === 1}
                <button class="btn btn-info btn-sm">
                  Watch Game
                </button>
              {:else}
                <button class="btn btn-secondary btn-sm" disabled>
                  Game Ended
                </button>
              {/if}
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>

<style>
  .game-lobby {
    padding: 20px;
    max-width: 1200px;
    margin: 0 auto;
  }

  .lobby-controls {
    display: flex;
    gap: 10px;
    align-items: center;
    margin-bottom: 20px;
    flex-wrap: wrap;
  }

  .connection-status {
    margin-left: auto;
    padding: 5px 10px;
    border-radius: 15px;
    font-size: 0.9em;
    font-weight: bold;
  }

  .connection-status.connected {
    background-color: #d4edda;
    color: #155724;
  }

  .connection-status.disconnected {
    background-color: #f8d7da;
    color: #721c24;
  }

  .games-container {
    margin-top: 20px;
  }

  .no-games {
    text-align: center;
    padding: 40px;
    color: #666;
  }

  .games-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 20px;
  }

  .game-card {
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 15px;
    background-color: #fff;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  }

  .game-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
  }

  .game-header h3 {
    margin: 0;
    font-size: 1.2em;
  }

  .game-status {
    padding: 4px 8px;
    border-radius: 12px;
    font-size: 0.8em;
    font-weight: bold;
    text-transform: uppercase;
  }

  .status-waiting {
    background-color: #fff3cd;
    color: #856404;
  }

  .status-playing {
    background-color: #d1ecf1;
    color: #0c5460;
  }

  .status-completed {
    background-color: #d4edda;
    color: #155724;
  }

  .game-details {
    margin-bottom: 15px;
  }

  .game-details p {
    margin: 5px 0;
    font-size: 0.9em;
  }

  .players-list {
    margin-bottom: 15px;
  }

  .players-list h4 {
    margin: 0 0 8px 0;
    font-size: 1em;
  }

  .players-list ul {
    list-style: none;
    padding: 0;
    margin: 0;
  }

  .player {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 4px 0;
    font-size: 0.9em;
  }

  .game-actions {
    display: flex;
    gap: 10px;
  }

  .btn {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.9em;
    transition: background-color 0.2s;
  }

  .btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  .btn-primary {
    background-color: #007bff;
    color: white;
  }

  .btn-primary:hover:not(:disabled) {
    background-color: #0056b3;
  }

  .btn-secondary {
    background-color: #6c757d;
    color: white;
  }

  .btn-secondary:hover:not(:disabled) {
    background-color: #545b62;
  }

  .btn-success {
    background-color: #28a745;
    color: white;
  }

  .btn-success:hover:not(:disabled) {
    background-color: #1e7e34;
  }

  .btn-info {
    background-color: #17a2b8;
    color: white;
  }

  .btn-info:hover:not(:disabled) {
    background-color: #117a8b;
  }

  .btn-sm {
    padding: 4px 8px;
    font-size: 0.8em;
  }

  .alert {
    padding: 10px;
    margin-bottom: 15px;
    border-radius: 4px;
    position: relative;
  }

  .alert-danger {
    background-color: #f8d7da;
    color: #721c24;
    border: 1px solid #f5c6cb;
  }

  .btn-close {
    position: absolute;
    top: 5px;
    right: 10px;
    background: none;
    border: none;
    font-size: 1.2em;
    cursor: pointer;
  }
</style>
