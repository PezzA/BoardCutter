<script lang="ts">
  import { onMount } from 'svelte';
  
  let games = [
    { id: '2048', name: '2048', description: 'Classic sliding puzzle game' }
  ];
  
  onMount(() => {
    console.log('GameList component mounted');
  });
</script>

<div class="game-list">
  <h1>Available Games</h1>
  <div class="games-container">
    {#each games as game}
      <div class="game-card">
        <h3>{game.name}</h3>
        <p>{game.description}</p>
        <a href="/twenty48" class="btn btn-primary">Play {game.name}</a>
      </div>
    {/each}
  </div>
</div>

<style>
  .game-list {
    padding: 20px;
  }
  
  .games-container {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 20px;
    margin-top: 20px;
  }
  
  .game-card {
    background: white;
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 20px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  }
  
  .btn {
    display: inline-block;
    padding: 8px 16px;
    text-decoration: none;
    border-radius: 4px;
    margin-top: 10px;
  }
  
  .btn-primary {
    background-color: #007bff;
    color: white;
  }
</style>
