<script lang="ts">
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";

  // SignalR connection variables
  let connection: signalR.HubConnection | null = $state(null);
  let connectionStatus = $state<
    "disconnected" | "reconnecting" | "connected" | "error"
  >("disconnected");
  let cells = $state<number[][]>([]);
  let score = $state(0);
  let status = $state(0);
  let gameId = $state("");

  // Game board constants
  const cellWidth = 75;
  const cellMargin = 5;
  const gridWidth = 4;

  const animationNewCellDelayMs = 100;
  const animationNewCellRevertMs = 300;
  const animationRemoveCellDelayMs = 50;
  const animationLockoutDurationMs = 300;

  let animLocked = $state(false);

  // Touch/Swipe variables
  let touchStartX = 0;
  let touchStartY = 0;
  let touchEndX = 0;
  let touchEndY = 0;

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
        console.log("Game Status: " + status);
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

  // Game board functions
  function drawGrid(grid: HTMLDivElement) {
    for (let x = 0; x < gridWidth; x++) {
      for (let y = 0; y < gridWidth; y++) {
        grid.appendChild(addCell(0, 0, x, y, cellWidth, true, false));
      }
    }
  }

  function handleTouchStart(e: TouchEvent) {
    if (e.touches.length === 1) {
      touchStartX = e.touches[0].clientX;
      touchStartY = e.touches[0].clientY;
    }
  }

  function handleTouchEnd(e: TouchEvent) {
    if (e.changedTouches.length === 1) {
      e.preventDefault();
      touchEndX = e.changedTouches[0].clientX;
      touchEndY = e.changedTouches[0].clientY;
      handleSwipe();
    }
  }

  function handleSwipe() {
    const dx = touchEndX - touchStartX;
    const dy = touchEndY - touchStartY;
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

    if (direction !== -1 && connection) {
      connection.invoke("Move", gameId, direction).catch(function (err: Error) {
        console.log(
          "Could not invoke method [Move] on signalR connection." +
            err.toString(),
        );
      });
    }
  }

  function tryAgain() {
    if (connection) {
      connection.invoke("StartNew").catch(function (err: Error) {
        console.error("Failed to start new game:", err);
      });
    }
  }

  function simulateGameEnd() {
    status = 3;
  }

  function toPixels(input: number): number {
    return input * (cellWidth + cellMargin) + cellMargin;
  }

  function moveCell(element: HTMLElement, x: number, y: number): void {
    element.style.top = toPixels(y) + "px";
    element.style.left = toPixels(x) + "px";
  }

  function getCellId(id: string): string {
    return `cellId-${id}`;
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

  function keydown(e: KeyboardEvent) {
    if (e.repeat) return;
    if (animLocked) return;

    if (
      e.key === "ArrowUp" ||
      e.key === "ArrowDown" ||
      e.key === "ArrowLeft" ||
      e.key === "ArrowRight" ||
      e.key === "w" ||
      e.key === "a" ||
      e.key === "s" ||
      e.key === "d"
    ) {
      e.preventDefault();
      const decodedKey = getDirectionFromKey(e.key);

      if (decodedKey !== -1 && connection) {
        connection.invoke("Move", gameId, decodedKey).catch(function (
          err: Error,
        ) {
          console.log(
            "Could not invoke method [Move] on signalR connection." +
              err.toString(),
          );
        });
      }
    }
  }

  function addCell(
    id: number,
    value: number,
    x: number,
    y: number,
    width: number,
    isBase: boolean,
    isMerged: boolean,
  ): HTMLDivElement {
    const node = document.createElement("div");
    node.id = getCellId(id.toString());
    node.classList.add("cell");
    node.classList.add(`cell-${value}`);

    if (!isMerged) {
      node.classList.add("newCell");
    }

    if (value !== 0) {
      node.innerText = value.toString();
    }

    node.style.zIndex = isBase ? "0" : id.toString();
    node.style.top = toPixels(y) + "px";
    node.style.left = toPixels(x) + "px";
    node.style.width = width + "px";
    node.style.height = width + "px";

    return node;
  }

  function drawCells() {
    const grid = document.getElementById("grid");

    if (cells.length === 0) {
      console.log("drawCells: No cells to draw");
      return;
    }

    if (!grid) {
      console.error("Grid element not found");
      return;
    }

    cells.forEach((cell: any) => {
      const cellElement = document.getElementById(getCellId(cell.Id));

      if (cellElement) {
        moveCell(cellElement, cell.Point.X, cell.Point.Y);
      } else {
        setTimeout(function () {
          const cellElement = addCell(
            cell.Id,
            cell.Value,
            cell.Point.X,
            cell.Point.Y,
            cellWidth,
            false,
            cell.Merged,
          );
          grid.appendChild(cellElement);

          if (!cell.Merged) {
            setTimeout(function () {
              cellElement.classList.remove("newCell");
            }, animationNewCellRevertMs);
          }
        }, animationNewCellDelayMs);
      }

      if (cell.Destroy === true) {
        const cellElement = document.getElementById(getCellId(cell.Id));
        if (cellElement) {
          cellElement.style.opacity = "0";
          setTimeout(function () {
            grid.removeChild(cellElement);
          }, animationRemoveCellDelayMs);
        }
      }
      if (cell.New || cell.Destroy) {
        return;
      }
    });
  }

  // Update Logic
  $effect(() => {
    animLocked = true;
    setTimeout(() => {
      animLocked = false;
    }, animationLockoutDurationMs);
    drawCells();
  });

  onMount(() => {
    setTimeout(() => {
      connectToSignalR();
    }, 200);

    // Initialize grid when cells are available
    const initGrid = () => {
      let grid = document.getElementById("grid") as HTMLDivElement | null;
      if (!grid) {
        setTimeout(initGrid, 100);
        return;
      }

      let gridWidthPX: number = 4 * cellWidth + 5 * cellMargin + 5;
      grid.style.height = gridWidthPX + "px";
      grid.style.width = gridWidthPX + "px";
      drawGrid(grid);
      console.log("Board component mounted with grid size:", gridWidthPX);
    };

    setTimeout(initGrid, 300);
  });
</script>

<svelte:window onkeydown={keydown} />

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
    <!-- Show game board if available -->
    {#if cells.length > 0}
      <div
        class="game-board"
        ontouchstart={handleTouchStart}
        ontouchend={handleTouchEnd}
      >
        <div class="score">Score: {score}</div>
        <div class="status">Status: {status}</div>
        <div
          class="grid"
          id="grid"
          class:locked={animLocked}
          class:unlocked={!animLocked}
          class:game-over={status === 3}
        ></div>

        {#if status === 3}
          <div class="game-over-message">Game Over</div>
          <button class="play-again-button" onclick={tryAgain}
            >Play Again</button
          >
        {/if}

        <button class="debug-button" onclick={simulateGameEnd}
          >Simulate Game End</button
        >
      </div>
    {/if}
  {/if}
</main>

<style>
  @import "./Twenty48.css";
</style>
