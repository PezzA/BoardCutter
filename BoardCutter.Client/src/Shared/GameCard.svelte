<script lang="ts">
  interface Props {
    game: any;
    connectionStatus: string;
    onJoinGame: (gameId: string) => void;
  }

  let { game, connectionStatus, onJoinGame }: Props = $props();
</script>

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

    {#if game.score !== undefined && game.score !== null}
      <div class="info-row">
        <span class="label">Score:</span>
        <span class="value score-value">{game.score}</span>
      </div>
    {/if}

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
      onclick={() => onJoinGame(game.id)}
      disabled={connectionStatus !== "connected" || game.status === 2}
    >
      {game.status === 2 ? "Full" : "Join Game"}
    </button>
  </div>
</div>

<style>
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

  .score-value {
    font-weight: 600;
    color: #2196f3;
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
