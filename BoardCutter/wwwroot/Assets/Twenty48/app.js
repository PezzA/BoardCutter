const CELL_WIDTH = 160;
const CELL_MARGIN = 10;

function toPixels(input) {
    return (input * (CELL_WIDTH + CELL_MARGIN)) + CELL_MARGIN;
}

function moveCell(element, x, y) {
    element.style.top = toPixels(y) + "px";
    element.style.left = toPixels(x) + "px";
}

function getCellId(id) {
    return `cellId-${id}`;
}

function addCell(id, value, x, y) {
    const node = document.createElement("div");

    node.id = getCellId(id);

    node.classList.add('cell');
    node.classList.add(`cell-${value}`);

    if (value !== 0) {
        node.innerText = value;
    }

    node.style.zIndex = id;
    node.style.top = toPixels(y) + "px";
    node.style.left = toPixels(x) + "px";

    const mainElement = document.getElementById("gameBoard");

    if (mainElement) {
        mainElement.appendChild(node);
    } else {
        console.log("NO EXISTO!!");
    }
}

window.initGrid = (dotNetHelper, cells) => {
    console.log('JS Game Init');

    window.addEventListener("keydown", function (e) {
        if (e.repeat) return;

        if (e.key === "ArrowUp" || e.key === "ArrowDown" || e.key === "ArrowLeft" || e.key === "ArrowRight") {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('Move', e.key);
        }
    });

    for (let x = 0; x < 4; x++) {
        for (let y = 0; y < 4; y++) {
            addCell(0, 0, x, y);
        }
    }
    
    drawCells(cells);
};

function drawCells(cells){
    cells.forEach(cell => {
        const cellElement = document.getElementById(getCellId(cell.id));

        if (cellElement) {
            moveCell(cellElement, cell.point.x, cell.point.y);
        } else {
            addCell(cell.id, cell.value, cell.point.x, cell.point.y);
        }
    });

    setTimeout(function () {
        cells.forEach(cell => {
            if (cell.destroy) {
                const deleteCell = document.getElementById(getCellId(cell.id));

                if (deleteCell) {
                    deleteCell.remove();
                }
            }
        });
    }, 100);
}
/*
 This is called from the blazor app each time we receive game state from the server.
 */
window.sendCells = (cells) => {
   drawCells(cells);
};