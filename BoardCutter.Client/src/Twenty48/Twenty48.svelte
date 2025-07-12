<script lang="ts">
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";
  // @ts-ignore - Svelte component import
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
          const url = new URL(window.location.href);
          url.searchParams.set("gameid", data.GameId);

          // Update URL without reloading the page
          history.pushState(null, "", url.toString());
        }
      } catch (err) {
        console.error("Failed to handle SetPlayerGame: " + err);
      }
    });

    connection.on("PublicVisible", (message: any) => {
      try {
        const data = JSON.parse(message);

        cells = data.Cells;
        score = data.Score;
        status = data.Status;
        gameId = data.GameId;

        if (data.Status === 3) {
          console.log("End of Game");
        }
      } catch (err) {
        console.error("Failed to handle PublicVisible: " + err);
      }
    });

    connection.on("PlayerStatus", async (message: any) => {
      console.log("PlayerStatus: " + message);

      const urlParams = new URLSearchParams(window.location.search);
      const gameId = urlParams.get("gameid") || "";
      console.log("GameId from query string: " + gameId);

      if (gameId === "") {
        console.log("No gameId found in query string, starting new game");
        await startNewGame();
      }
    });

    try {
      await connection.start();
      connectionStatus = "connected";

      // Get gameid from query string if it exists, else empty string
      const urlParams = new URLSearchParams(window.location.search);
      const gameId = urlParams.get("gameid") || "";
      console.log("GameId from query string: " + gameId);

      try {
        await connection.invoke("CheckPlayerStatus", gameId);
        console.log("CheckPlayerStatus called with gameId: " + gameId);
      } catch (err) {
        console.error("Failed to call CheckPlayerStatus: " + err);
      }
    } catch (err) {
      connectionStatus = "error";
      console.error("SignalR connection error: " + err);
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
  <div
    class="status-bar"
    class:connected={connectionStatus === "connected"}
    class:reconnecting={connectionStatus === "reconnecting"}
    class:disconnected={connectionStatus === "disconnected" ||
      connectionStatus === "error"}
  ></div>

  {#if status === 0}
    <div>Loading</div>
  {:else}
    <Board {cells} {score} {gameId} {connection} {status} />
  {/if}
</main>

<style>
  .game-window {
    margin: 0;
  }
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
    text-align: center;
  }

  .status-bar {
    height: 4px;
    width: 100vw;
    position: fixed;
    top: 0;
    left: 0;
    z-index: 1000;
    transition: background-color 0.3s ease;
  }

  .status-bar.connected {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #4caf50 60px,
      #4caf50 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }

  .status-bar.reconnecting {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #ffb300 60px,
      #ffb300 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }

  .status-bar.disconnected {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #f44336 60px,
      #f44336 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }
</style>
