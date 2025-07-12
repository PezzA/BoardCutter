<script lang="ts">
  import { onMount } from "svelte";

  let { cells = [], score = 0, gameId, connection, status = 0 } = $props();

  const cellWidth = 75;
  const cellMargin = 5;
  const gridWidth = 4;

  const animationNewCellDelayMs = 100;
  const animationNewCellRevertMs = 300;
  const animationRemoveCellDelayMs = 50;
  // Whilst animating, input is locked, so we are not updating state whilst still
  // animateing from the previous state.
  const animationLockoutDurationMs = 300; // milliseconds

  let animLocked = $state(false);

  // First Draw
  onMount(() => {
    let grid = document.getElementById("grid") as HTMLDivElement | null;

    if (!grid) {
      console.error("Grid element not found");
      return;
    }

    let gridWidthPX: number = 4 * cellWidth + 5 * cellMargin + 5;

    grid.style.height = gridWidthPX + "px";
    grid.style.width = gridWidthPX + "px";

    drawGrid(grid);

    console.log("Board component mounted with grid size:", gridWidthPX);
  });

  function drawGrid(grid: HTMLDivElement) {
    for (let x = 0; x < gridWidth; x++) {
      for (let y = 0; y < gridWidth; y++) {
        grid.appendChild(addCell(0, 0, x, y, cellWidth, true, false));
      }
    }
  }

  // Update Logic
  $effect(() => {
    animLocked = true;
    setTimeout(() => {
      animLocked = false;
    }, animationLockoutDurationMs);

    drawCells();
  });

  // Touch/Swipe support for mobile - only works on grid
  let touchStartX = 0;
  let touchStartY = 0;
  let touchEndX = 0;
  let touchEndY = 0;

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
    const minDistance = 30; // Minimum swipe distance

    if (absDx < minDistance && absDy < minDistance) return;

    let direction = -1;
    if (absDx > absDy) {
      direction = dx > 0 ? 3 : 2; // Right : Left
    } else {
      direction = dy > 0 ? 1 : 0; // Down : Up
    }

    if (direction !== -1) {
      connection.invoke("Move", gameId, direction).catch(function (err: Error) {
        console.log(
          "Could not invoke method [Move] on signalR connection." +
            err.toString(),
        );
      });
    }
  }

  function tryAgain() {
    connection.invoke("StartNew").catch(function (err: Error) {
      console.error("Failed to start new game:", err);
    });
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

      //("Move: " + "[GameId:" + classClosure.gameId + "]" + " [Direction:" + decodedKey + "]");

      if (decodedKey !== -1) {
        connection.invoke("Move", gameId, decodedKey).catch(function (
          err: Error,
        ) {
          console.log(
            "Could not invoke method [Move] on signalR connection." +
              err.toString(),
          );
        });

        //classClosure.logger.logDebug("Processing Move: " + decodedKey);
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

    // first thing, move any cells that are moving
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
</script>

<svelte:window onkeydown={keydown} />

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
      <button class="play-again-button" onclick={tryAgain}>Play Again</button>
    {/if}

    <button class="debug-button" onclick={simulateGameEnd}
      >Simulate Game End</button
    >
  </div>
{/if}

<style>
  .game-board {
    margin: 1rem auto 0;
    touch-action: none;
    display: inline-block;
  }

  .score {
    font-size: 1.5rem;
    margin-bottom: 0.5rem;
    text-align: center;
  }

  .grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    grid-gap: 10px;
    margin: 1rem 0;
    background-color: #bbada0;
    position: relative;
    border: 3px solid transparent;
    border-radius: 5px;
    transition: border-color 0.3s ease;
    box-sizing: border-box;
  }

  .grid.locked {
    border-color: #ffb300; /* amber border when locked */
  }

  .grid.unlocked {
    border-color: #4caf50; /* green border when unlocked */
  }

  :global(.cell) {
    box-sizing: border-box;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
    font-weight: bold;
    background: rgba(238, 228, 218, 0.35);
    border-radius: 5px;
    color: black;
    position: absolute;
    transition: all 0.1s ease-in-out;
  }

  :global(.newCell) {
    animation-name: rotate;
    animation-duration: 0.3s;
  }

  @keyframes -global-rotate {
    0% {
      transform: scale(0.1);
      opacity: 0;
    }

    80% {
      transform: scale(1.2);
      opacity: 1;
    }

    100% {
      transform: scale(1);
      opacity: 1;
    }
  }

  :global(.cell-2) {
    background: #eee4da;
  }

  :global(.cell-4) {
    background: #eee1c9;
  }

  :global(.cell-8) {
    color: #f9f6f2;
    background: #f3b27a;
  }

  :global(.cell-16) {
    color: #f9f6f2;
    background: #f69664;
  }

  :global(.cell-32) {
    color: #f9f6f2;
    background: #f77c5f;
  }

  :global(.cell-64) {
    color: #f9f6f2;
    background: #f75f3b;
  }

  :global(.cell-128) {
    color: #f9f6f2;
    background: #edd073;
  }

  :global(.cell-256) {
    color: #f9f6f2;
    background: #edcc62;
  }

  .grid.game-over {
    opacity: 0.3;
    filter: grayscale(50%);
    pointer-events: none;
  }

  .game-over-message {
    text-align: center;
    font-size: 2rem;
    font-weight: bold;
    color: #333;
    margin: 1rem 0;
  }

  .play-again-button {
    display: block;
    margin: 1rem auto;
    background-color: #4caf50;
    color: white;
    border: none;
    padding: 0.75rem 2rem;
    font-size: 1.1rem;
    border-radius: 5px;
    cursor: pointer;
    transition: background-color 0.3s ease;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
  }

  .play-again-button:hover {
    background-color: #45a049;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3);
  }

  .debug-button {
    display: block;
    margin: 0.5rem auto;
    background-color: #ff9800;
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    font-size: 0.9rem;
    border-radius: 3px;
    cursor: pointer;
    transition: background-color 0.3s ease;
    opacity: 0.7;
  }

  .debug-button:hover {
    background-color: #f57c00;
    opacity: 1;
  }
</style>
