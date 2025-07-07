<style>
    .game-board {
        margin-top: 1rem;
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
    
    .cell-0 {

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
</style>

<script lang="ts">
    import {onMount} from 'svelte';

    let {cells = [], score = 0, gameId, connection} = $props();

    let cellWidth = 75;
    let cellMargin = 5;
    let gridWidth = 4;


    // First Draw
    onMount(() => {
        let grid = document.getElementById("grid");
        let gridWidthPX: number = (4 * cellWidth) + (5 * cellMargin);
        grid.style.height = gridWidthPX + "px";
        grid.style.width = gridWidthPX + "px";
    });

    // Update Logic
    $effect(() => {
        drawCells();
    });

    function toPixels(input: number): number {
        return (input * (cellWidth + cellMargin)) + cellMargin;
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

        if (e.key === "ArrowUp" || e.key === "ArrowDown" || e.key === "ArrowLeft" || e.key === "ArrowRight" ||
            e.key === "w" || e.key === "a" || e.key === "s" || e.key === "d") {
            e.preventDefault();

            const decodedKey = getDirectionFromKey(e.key);

            //("Move: " + "[GameId:" + classClosure.gameId + "]" + " [Direction:" + decodedKey + "]");

            if (decodedKey !== -1) {
                connection
                    .invoke("Move", gameId, decodedKey)
                    .catch(function (err: Error) {
                        console.log("Could not invoke method [Move] on signalR connection." + err.toString());
                    });

                //classClosure.logger.logDebug("Processing Move: " + decodedKey);
            }
        }
    }

    function addCell(id: number, value: number, x: number, y: number, width: number, isBase: boolean, isMerged: boolean): HTMLDivElement {
        const node = document.createElement("div");

        node.id = getCellId(id.toString());

        node.classList.add('cell');
        node.classList.add(`cell-${value}`);

        if (!isMerged) {
            node.classList.add('newCell');
        }

        if (value !== 0) {
            node.innerText = value.toString();
        }

        node.style.zIndex = isBase ? 0 : id.toString();
        node.style.top = toPixels(y) + "px";
        node.style.left = toPixels(x) + "px";
        node.style.width = width + "px";
        node.style.height = width + "px";

        return node;
    }

    function drawCells() {
        const grid = document.getElementById("grid");

        if (cells.length === 0) {
            push("drawCells: No cells to draw");
            return;
        }

        // first thing, move any cells that are moving
        cells.forEach((cell: any) => {
            const cellElement = document.getElementById(getCellId(cell.Id));

            if (cellElement) {
                moveCell(cellElement, cell.Point.X, cell.Point.Y);
            } else {
                setTimeout(function () {
                    const cellElement = addCell(cell.Id, cell.Value, cell.Point.X, cell.Point.Y, cellWidth, false, cell.Merged);
                    grid.appendChild(cellElement);

                    if(!cell.Merged)
                    {
                        setTimeout(function () {
                            cellElement.classList.remove('newCell');
                        }, 300);
                    }
                }, 100);
            }

            if (cell.Destroy === true) {
                const cellElement = document.getElementById(getCellId(cell.Id));
                
                if(cellElement) {
                    cellElement.style.opacity = "0";
                    
                    setTimeout(function () {
                        grid.removeChild(cellElement);
                    }, 50)
                }
                

            }
            if (cell.New || cell.Destroy) {
                return;
            }
        });
    }
</script>

<svelte:window onkeydown={keydown}/>

<!-- Show game board if available -->
{#if cells.length > 0}
    <div class="game-board">
        <div class="score">Score: {score}</div>
        <div class="grid" id="grid">
        </div>
    </div>
{/if}
