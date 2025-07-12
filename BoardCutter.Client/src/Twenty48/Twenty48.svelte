<script lang="ts">
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";
  // @ts-ignore - Svelte component import
  import LoggerPanel from "../Shared/Logger.svelte";
  import Board from "./Board.svelte";

  // Using regular variables in Svelte 5
  let connection: signalR.HubConnection | null = null;
  let connectionStatus:
    | "disconnected"
    | "reconnecting"
    | "connected"
    | "error" = "disconnected";
  let cells: number[][] = [];
  let score: number = 0;
  let status = 0;
  let gameId: string = "";

  let loggerPanelRef: any = null;
  let showLogger = false;
  let userFromSetPlayerGame: string = "";

  function pushLog(text: string, cssClass: string) {
    if (loggerPanelRef && loggerPanelRef.addMessage) {
      loggerPanelRef.addMessage({ text, cssClass });
    }
  }

  function toggleLogger() {
    showLogger = !showLogger;
  }

  function copyToClipboard(text: string) {
    navigator.clipboard
      .writeText(text)
      .then(() => {
        pushLog("User value copied to clipboard", "log-debug");
      })
      .catch((err) => {
        pushLog("Failed to copy user value: " + err, "log-error");
      });
  }

  async function connectToSignalR(): Promise<void> {
    connection = new signalR.HubConnectionBuilder()
      .withUrl("/twenty48hub", {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    connection.onclose(() => (connectionStatus = "disconnected"));
    connection.onreconnecting(() => (connectionStatus = "reconnecting"));
    connection.onreconnected(() => (connectionStatus = "connected"));

    connection.on("SetPlayerGame", (message: any) => {
      try {
        const data =
          typeof message === "string" ? JSON.parse(message) : message;
        if (data && data.GameId) {
          // Store the user value from the message
          userFromSetPlayerGame = data.User || "";

          const url = new URL(window.location.href);
          url.searchParams.set("gameid", data.GameId);
          window.location.href = url.toString();
        }
      } catch (err) {
        pushLog("Failed to handle SetPlayerGame: " + err, "log-error");
      }
    });

    connection.on("PublicVisible", (message: any) => {
      try {
        const data = JSON.parse(message);
        pushLog("PublicVisible: " + message, "log-down");

        cells = data.Cells;

        score = data.Score;

        status = data.Status;
        gameId = data.GameId;

        pushLog("PubVisible: Done", "log-debug");

        if (data.Status === 3) {
          pushLog("End of Game", "log-debug");

          //that.windowElements.gameBoard.style.opacity = "0.3";
          //that.removeEventListeners();
          //that.windowElements.gameOver.style.opacity = "1";
        }
      } catch (err) {
        pushLog("Failed to handle PublicVisible: " + err, "log-error");
      }
    });

    try {
      await connection.start();
      connectionStatus = "connected";

      // Get gameid from query string if it exists, else empty string
      const urlParams = new URLSearchParams(window.location.search);
      const gameId = urlParams.get("gameid") || "";

      try {
        await connection.invoke("CheckPlayerStatus", gameId);
        pushLog("CheckPlayerStatus called with gameId: " + gameId, "log-up");
      } catch (err) {
        pushLog("Failed to call CheckPlayerStatus: " + err, "log-error");
      }
    } catch (err) {
      connectionStatus = "error";
      pushLog("SignalR connection error: " + err, "log-error");
    }
  }

  async function startNewGame(): Promise<void> {
    if (connectionStatus === "connected" && connection) {
      try {
        await connection.invoke("StartNew");
      } catch (err) {
        console.error("Failed to start new game:", err);
      }
    }
  }

  onMount(() => {
    setTimeout(() => {
      connectToSignalR();
    }, 200);
  });
</script>

<main class="game-window">
  <div class="status">SignalR status: {connectionStatus}</div>

  {#if status === 0}
    {#if connectionStatus === "connected"}
      <button on:click={startNewGame} style="min-width:150px"
        >Start new Game!!!</button
      >
    {/if}
  {:else}
    <Board {cells} {score} {gameId} {connection} />
  {/if}

  {#if showLogger}
    <div class="cookie-section">
      <div class="cookie-info">
        <strong>User (from SetPlayerGame):</strong>
        <span class="cookie-value"
          >{userFromSetPlayerGame || "Not received yet"}</span
        >
        <button
          class="copy-button"
          on:click={() => copyToClipboard(userFromSetPlayerGame)}
          disabled={!userFromSetPlayerGame}
        >
          Copy
        </button>
      </div>
    </div>
    <LoggerPanel bind:this={loggerPanelRef} />
  {/if}

  <footer>
    <button class="debug-link" on:click={toggleLogger}>
      {showLogger ? "Hide Debug" : "Show Debug"}
    </button>
  </footer>
</main>

<style>
  :global(body) {
    margin: 0;
    min-height: 100vh;
    box-sizing: border-box;
    border: none;
  }

  :global(.signalr-top-border) {
    border-top: 8px solid #ffb300; /* default amber */
    transition: border-color 0.3s;
  }

  :global(.signalr-top-border.connected) {
    border-top-color: #4caf50; /* green */
  }

  :global(.signalr-top-border.not-connected) {
    border-top-color: #ffb300; /* amber */
  }

  main {
    padding: 1rem;
    font-family: Arial, sans-serif;
    max-width: 800px;
    margin: 2em auto;
    border-radius: 10px;
    border-color: red;
    border-width: 10px;
  }

  .status {
    margin-bottom: 1em;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }

  footer {
    margin-top: 2rem;
    text-align: center;
    padding: 1rem 0;
  }

  .debug-link {
    background: none;
    border: none;
    color: #666;
    cursor: pointer;
    font-size: 0.9rem;
    text-decoration: underline;
    padding: 0.5rem 1rem;
  }

  .debug-link:hover {
    color: #333;
    background-color: #f5f5f5;
    border-radius: 4px;
  }

  .cookie-section {
    background-color: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 1rem;
    margin-bottom: 1rem;
  }

  .cookie-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
  }

  .cookie-value {
    font-family: monospace;
    background-color: #e9ecef;
    padding: 0.25rem 0.5rem;
    border-radius: 3px;
    word-break: break-all;
    flex: 1;
    min-width: 200px;
  }

  .copy-button {
    background-color: #007bff;
    color: white;
    border: none;
    padding: 0.25rem 0.75rem;
    border-radius: 3px;
    cursor: pointer;
    font-size: 0.875rem;
  }

  .copy-button:hover:not(:disabled) {
    background-color: #0056b3;
  }

  .copy-button:disabled {
    background-color: #6c757d;
    cursor: not-allowed;
  }
</style>
