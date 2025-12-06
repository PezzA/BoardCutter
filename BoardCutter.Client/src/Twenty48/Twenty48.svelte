<script lang="ts">
  import { onMount } from "svelte";
  import { fade } from "svelte/transition";
  import { quintOut } from "svelte/easing";
  import StatusBar from "../Shared/StatusBar.svelte";
  import {
    createReactiveSignalRConnection,
    type ConnectionStatus,
  } from "../Shared/signalr.js";

  // TypeScript interfaces for game state
  interface Point {
    X: number;
    Y: number;
  }

  interface Cell {
    Id: number;
    Value: number;
    Point: Point;
    New: boolean;
    Merged: boolean;
    Destroy: boolean;
  }

  // SignalR connection
  const signalRConnection = createReactiveSignalRConnection({
    hubUrl: "/twenty48hub",
    withCredentials: true,
    automaticReconnect: true,
  });

  // Game state
  let connectionStatus = $state<ConnectionStatus>("disconnected");
  let cells = $state<Cell[]>([]);
  let score = $state(0);
  let status = $state(0);
  let gameId = $state("");

  // Subscribe to status changes
  signalRConnection.onStatusChange((newStatus) => {
    connectionStatus = newStatus;
  });

  // Game board constants
  const cellWidth = 75;
  const cellMargin = 5;
  const gridWidth = 4;
  const animationLockoutDurationMs = 300;

  let animLocked = $state(false);

  // Touch/Swipe variables
  let touchStartX = 0;
  let touchStartY = 0;

  // Computed grid size
  const gridSizePx = gridWidth * cellWidth + (gridWidth + 1) * cellMargin;

  // Helper functions
  function toPixels(input: number): number {
    return input * (cellWidth + cellMargin) + cellMargin;
  }

  function getDirectionFromKey(key: string): number {
    switch (key) {
      case "ArrowUp":
      case "w":
        return 0;
      case "ArrowDown":
      case "s":
        return 1;
      case "ArrowLeft":
      case "a":
        return 2;
      case "ArrowRight":
      case "d":
        return 3;
      default:
        return -1;
    }
  }

  // SignalR connection and handlers
  async function connectToSignalR(): Promise<void> {
    try {
      // Set up event handlers
      signalRConnection.on("SetPlayerGame", (message: any) => {
        try {
          const data =
            typeof message === "string" ? JSON.parse(message) : message;
          if (data && data.GameId) {
            const url = new URL(window.location.href);
            url.searchParams.set("gameid", data.GameId);
            history.pushState(null, "", url.toString());
          }
        } catch (err) {
          console.error("Failed to handle SetPlayerGame:", err);
        }
      });

      signalRConnection.on("PublicVisible", (message: any) => {
        try {
          const data = JSON.parse(message);
          cells = data.Cells || [];
          score = data.Score || 0;
          status = data.Status || 0;
          gameId = data.GameId || "";

          if (data.Status === 3) {
            console.log("Game Over");
          }
        } catch (err) {
          console.error("Failed to handle PublicVisible:", err);
        }
      });

      signalRConnection.on("PlayerStatus", async (message: any) => {
        console.log("PlayerStatus:", message);
        const urlParams = new URLSearchParams(window.location.search);
        const currentGameId = urlParams.get("gameid") || "";

        if (currentGameId === "") {
          console.log("Starting new game");
          await startNewGame();
        }
      });

      // Connect to hub
      await signalRConnection.connect();

      const urlParams = new URLSearchParams(window.location.search);
      const currentGameId = urlParams.get("gameid") || "";

      await signalRConnection.invoke("CheckPlayerStatus", currentGameId);
    } catch (err) {
      console.error("SignalR connection error:", err);
    }
  }

  async function startNewGame(): Promise<void> {
    if (connectionStatus === "connected" && signalRConnection.connection) {
      try {
        await signalRConnection.invoke("StartNew");
      } catch (err) {
        console.error("Failed to start new game:", err);
      }
    }
  }

  function tryAgain() {
    startNewGame();
  }

  // Input handlers
  function handleTouchStart(e: TouchEvent) {
    if (e.touches.length === 1) {
      touchStartX = e.touches[0].clientX;
      touchStartY = e.touches[0].clientY;
    }
  }

  function handleTouchEnd(e: TouchEvent) {
    if (e.changedTouches.length === 1) {
      e.preventDefault();
      const touchEndX = e.changedTouches[0].clientX;
      const touchEndY = e.changedTouches[0].clientY;
      handleSwipe(touchEndX - touchStartX, touchEndY - touchStartY);
    }
  }

  function handleSwipe(dx: number, dy: number) {
    const absDx = Math.abs(dx);
    const absDy = Math.abs(dy);
    const minDistance = 30;

    if (absDx < minDistance && absDy < minDistance) return;

    let direction = -1;
    if (absDx > absDy) {
      direction = dx > 0 ? 3 : 2; // Right : Left
    } else {
      direction = dy > 0 ? 1 : 0; // Down : Up
    }

    if (direction !== -1) {
      sendMove(direction);
    }
  }

  function keydown(e: KeyboardEvent) {
    if (e.repeat || animLocked) return;

    if (
      ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "w", "a", "s", "d"].includes(e.key)
    ) {
      e.preventDefault();
      const direction = getDirectionFromKey(e.key);
      if (direction !== -1) {
        sendMove(direction);
      }
    }
  }

  async function sendMove(direction: number) {
    if (signalRConnection.connection && gameId) {
      try {
        await signalRConnection.invoke("Move", gameId, direction);
      } catch (err) {
        console.error("Move failed:", err);
      }
    }
  }

  // Animation lockout effect
  $effect(() => {
    if (cells.length > 0) {
      animLocked = true;
      setTimeout(() => {
        animLocked = false;
      }, animationLockoutDurationMs);
    }
  });

  // Custom transition for new cells with pop-in effect
  function popIn(node: HTMLElement, { delay = 0 }: { delay?: number }) {
    return {
      delay,
      duration: 300,
      easing: quintOut,
      css: (t: number) => `
        transform: scale(${t});
        opacity: ${t};
      `,
    };
  }

  onMount(() => {
    setTimeout(() => {
      connectToSignalR();
    }, 200);
  });
</script>

<svelte:window onkeydown={keydown} />

<main class="game-window">
  <StatusBar {connectionStatus} />

  {#if status === 0}
    <div class="loading">Loading...</div>
  {:else if cells.length > 0}
    <div
      class="game-board"
      ontouchstart={handleTouchStart}
      ontouchend={handleTouchEnd}
    >
      <div class="score">Score: {score}</div>

      <div
        class="grid"
        style="width: {gridSizePx}px; height: {gridSizePx}px;"
        class:locked={animLocked}
        class:unlocked={!animLocked}
        class:game-over={status === 3}
      >
        <!-- Base grid cells (always visible) -->
        {#each Array(gridWidth * gridWidth) as _, i}
          <div
            class="cell cell-base"
            style="
              left: {toPixels(i % gridWidth)}px;
              top: {toPixels(Math.floor(i / gridWidth))}px;
              width: {cellWidth}px;
              height: {cellWidth}px;
            "
          ></div>
        {/each}

        <!-- Active game cells (reactive) -->
        {#each cells.filter((c) => !c.Destroy) as cell (cell.Id)}
          <div
            class="cell cell-{cell.Value}"
            class:new-cell={cell.New && !cell.Merged}
            class:merged-cell={cell.Merged}
            style="
              left: {toPixels(cell.Point.X)}px;
              top: {toPixels(cell.Point.Y)}px;
              width: {cellWidth}px;
              height: {cellWidth}px;
              z-index: {cell.Id};
            "
            in:popIn={{ delay: 100 }}
            out:fade={{ duration: 50 }}
          >
            {cell.Value}
          </div>
        {/each}
      </div>

      {#if status === 3}
        <div class="game-over-overlay">
          <div class="game-over-message">Game Over</div>
          <button class="play-again-button" onclick={tryAgain}>
            Play Again
          </button>
        </div>
      {/if}
    </div>
  {/if}
</main>

<style>
  @import "./Twenty48.css";

  .loading {
    text-align: center;
    padding: 2rem;
    font-size: 1.2rem;
  }

  .grid {
    position: relative;
    background-color: #bbada0;
    border-radius: 6px;
    padding: 5px;
  }

  .cell {
    position: absolute;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    font-size: 2rem;
    border-radius: 3px;
    transition: all 0.15s ease-in-out;
  }

  .cell-base {
    background-color: rgba(238, 228, 218, 0.35);
    z-index: 0;
  }

  /* New cell animation */
  .new-cell {
    animation: pop-in 0.3s ease-out;
  }

  @keyframes pop-in {
    0% {
      transform: scale(0);
    }
    50% {
      transform: scale(1.1);
    }
    100% {
      transform: scale(1);
    }
  }

  /* Merged cell pulse */
  .merged-cell {
    animation: pulse 0.2s ease-in-out;
  }

  @keyframes pulse {
    0%,
    100% {
      transform: scale(1);
    }
    50% {
      transform: scale(1.05);
    }
  }

  /* Cell colors */
  .cell-2 {
    background-color: #eee4da;
    color: #776e65;
  }
  .cell-4 {
    background-color: #ede0c8;
    color: #776e65;
  }
  .cell-8 {
    background-color: #f2b179;
    color: #f9f6f2;
  }
  .cell-16 {
    background-color: #f59563;
    color: #f9f6f2;
  }
  .cell-32 {
    background-color: #f67c5f;
    color: #f9f6f2;
  }
  .cell-64 {
    background-color: #f65e3b;
    color: #f9f6f2;
  }
  .cell-128 {
    background-color: #edcf72;
    color: #f9f6f2;
    font-size: 1.75rem;
  }
  .cell-256 {
    background-color: #edcc61;
    color: #f9f6f2;
    font-size: 1.75rem;
  }
  .cell-512 {
    background-color: #edc850;
    color: #f9f6f2;
    font-size: 1.75rem;
  }
  .cell-1024 {
    background-color: #edc53f;
    color: #f9f6f2;
    font-size: 1.5rem;
  }
  .cell-2048 {
    background-color: #edc22e;
    color: #f9f6f2;
    font-size: 1.5rem;
  }
  .cell-4096 {
    background-color: #3c3a32;
    color: #f9f6f2;
    font-size: 1.25rem;
  }

  .game-over-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(238, 228, 218, 0.73);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    border-radius: 6px;
  }

  .game-over-message {
    font-size: 3rem;
    font-weight: bold;
    color: #776e65;
    margin-bottom: 1rem;
  }

  .play-again-button {
    background-color: #8f7a66;
    color: #f9f6f2;
    border: none;
    padding: 1rem 2rem;
    font-size: 1.2rem;
    font-weight: bold;
    border-radius: 3px;
    cursor: pointer;
    transition: background-color 0.2s;
  }

  .play-again-button:hover {
    background-color: #9f8a76;
  }
</style>
