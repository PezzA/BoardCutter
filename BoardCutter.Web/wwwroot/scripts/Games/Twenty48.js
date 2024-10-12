
const logContainer = document.getElementById('debugLog');

const GAME_ID_PARAM = 'gameid';

function logText(text, level) {
    var logElement = document.createElement('p');
    logElement.textContent = new Date().toLocaleTimeString() + ': ' + text;
    logElement.classList.add('log' + level);
    logContainer.appendChild(logElement);
};

function logUp(text) {
    logText(text, "Up");
}

function logDown(text) {
    logText(text, "Down");
}

function logSuccess(text) {
    logText(text, "Success");
}

function logInfo(text) {
    logText(text, "Info");
}

function logError(text) {
    logText(text, "Error");
}

function logWarn(text) {
    logText(text, "Warn");
}

function logDebug(text) {
    logText(text, "Debug");
}

function ShowStart(showStart, showRunning) {
    document.getElementById('newGame').hidden = showStart ? '' : 'hidden';
    document.getElementById('runningGame').hidden = showRunning ? '' : 'hidden';
}

window.Twenty48 = {
    connection: {},
    setReady: {},
    gameId: '',

    startGame: () => {
        logUp("StartNew:");
        window.Twenty48.connection
            .invoke("StartNew")
            .catch(function (err) {
                logError("Could not invoke method [Ping] on signalR connection." + err.toString());
            });
    },

    connect: (hubUrl, gameId) => {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // register our callbacks when the hub sends us an event
        connection
            .on("PlayerStatus", function (message) {
                logDown("PlayerStatus: " + message);
            });

        // register our callbacks when the hub sends us an event
        connection
            .on("SetPlayerGame", function (message) {
                var data = JSON.parse(message);
                logDown("SetPlayerGame: " + message);
                window.location.href += `?${GAME_ID_PARAM}=` + data.GameId;
            });

        connection
            .on("PublicVisible", function (message) {
                var data = JSON.parse(message);
                logDown("PublicVisible: " + message);
                modCells = data.Cells;

                document.getElementById('scoreValue').innerText = data.Score;
                drawCells();
                logDebug('PubVisible: Done');
            });

        connection
            .start()
            .then(function () {
                logSuccess("SignalR Connection Established");
                this.Twenty48.setReady();

                logUp("CheckPlayerStatus:", gameId ?? "");
                window.Twenty48.connection
                    .invoke("CheckPlayerStatus", gameId ?? "")
                    .catch(function (err) {
                        logError("Could not invoke method [] on signalR connection." + err.toString());
                    });
            })
            .catch(function (err) {
                logError("could not establish a signalR connection." + err.toString());
            });

        window.Twenty48.connection = connection;
    },

    setup: (hubUrl, readyHandler) => {
        window.Twenty48.setReady = readyHandler;
        
        const urlParams = new URLSearchParams(window.location.search);
        const myParam = urlParams.get(GAME_ID_PARAM);

        if (myParam) {
            logDebug("GameId, found, showing screen: " + myParam);
            window.Twenty48.gameId = myParam;
            ShowStart(false, true);
            initGrid();
        } else {
            logDebug("No GameId found. Showing Setup");
            ShowStart(true, false);
       
        }

        window.Twenty48.connect(hubUrl, myParam);
    }
};

let CELL_WIDTH = 80;
let CELL_MARGIN = 10;
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

var prevSize = null;
var sizeChanged = false;

function resize(forceDraw) {
    var bounds = document.getElementById('gameBoard').getBoundingClientRect();

    if (bounds.width == 200 && prevSize != 200) {
        CELL_WIDTH = 45;
        CELL_MARGIN = 4;
        sizeChanged = true;
    } else if (bounds.width == 300 && prevSize != 300) {
        CELL_WIDTH = 70;
        CELL_MARGIN = 4;
        sizeChanged = true;
    } else if (bounds.width == 370 && prevSize != 370) {
        CELL_WIDTH = 80;
        CELL_MARGIN = 10;
        sizeChanged = true;
    }

    if (sizeChanged || forceDraw) {
        console.log('redraw');
        prevSize = bounds.width;
        sizeChanged = false;

        var debugElement = document.getElementById('debugText');
        debugElement.innerText = `Width: ${bounds.width}, Cell Width: ${CELL_WIDTH}, Margin: ${CELL_MARGIN}`;

        document.getElementById('gameBoard').innerHTML = '';

        for (let x = 0; x < 4; x++) {
            for (let y = 0; y < 4; y++) {
                addCell(0, 0, x, y);
            }
        }

        drawCells();
    }
}

let modCells = [];

function getDirectionFromKey(key) {

    switch (key) {
        case "ArrowUp":
            return 0;
        case "ArrowDown":
            return 1;
        case "ArrowLeft":
            return 2;
        case "ArrowRight":
            return 3;
        default:
            return -1;
    }
}
function initGrid()
{
    logDebug('Initalising Game');

    window.addEventListener('resize', function () {
        resize();
    }, true);


    const gameBoard = document.getElementById("gameBoard");

    // https://stackoverflow.com/questions/2264072/detect-a-finger-swipe-through-javascript-on-the-iphone-and-android
    gameBoard.addEventListener('touchstart', handleTouchStart, { passive: true });
    gameBoard.addEventListener('touchend', handleTouchEnd, { passive: true });

    window.addEventListener("keydown", function (e) {
        if (e.repeat) return;

        if (e.key === "ArrowUp" || e.key === "ArrowDown" || e.key === "ArrowLeft" || e.key === "ArrowRight") {
            e.preventDefault();

            var decodedKey = getDirectionFromKey(e.key);

            logUp("Move: " + "[GameId:" + window.Twenty48.gameId + "]" + " [Direction:" + decodedKey + "]");

            if (decodedKey !== -1) {
                window.Twenty48.connection
                    .invoke("Move", window.Twenty48.gameId, decodedKey)
                    .catch(function (err) {
                        logError("Could not invoke method [Move] on signalR connection." + err.toString());
                    });

                logDebug("Procssing Move: " + decodedKey);
            }
        }
    });

    resize(true);
};

function drawGame() {
    for (let x = 0; x < 4; x++) {
        for (let y = 0; y < 4; y++) {
            addCell(0, 0, x, y);
        }
    }

    drawCells();
}

var xDown = null;
var yDown = null;

function getTouches(evt) {
    return evt.touches ||             // browser API
        evt.originalEvent.touches; // jQuery
}

function handleTouchStart(evt) {
    console.log(evt);
    evt.preventDefault();
    const firstTouch = getTouches(evt)[0];
    xDown = firstTouch.clientX;
    yDown = firstTouch.clientY;
};

function handleTouchEnd(evt) {
    console.log(evt);
    evt.preventDefault();
    if (!xDown || !yDown) {
        return;
    }

    var xUp = evt.changedTouches[0].clientX;
    var yUp = evt.changedTouches[0].clientY;

    var xDiff = xDown - xUp;
    var yDiff = yDown - yUp;

    let dir = -1;

    if (Math.abs(xDiff) > Math.abs(yDiff)) {/*most significant*/
        if (xDiff > 0) {
            dir = 2;
        } else {
            dir = 3;
        }
    } else {
        if (yDiff > 0) {
            dir = 0;
        } else {
            dir = 1;
        }
    }

    if (dir !== -1) {
        window.Twenty48.connection
            .invoke("Move", window.Twenty48.gameId, dir)
            .catch(function (err) {
                logError("Could not invoke method [Move] on signalR connection." + err.toString());
            });

        logDebug("Procssing Move: " + dir);
    }

    /* reset values */
    xDown = null;
    yDown = null;
};

function drawCells() {

    if (!modCells) {
        logWarn("drawCells: No cells to draw");
        return;
    }

    modCells.forEach(cell => {
        const cellElement = document.getElementById(getCellId(cell.Id));

        if (cellElement) {
            moveCell(cellElement, cell.Point.X, cell.Point.Y);
        } else {
            addCell(cell.Id, cell.Value, cell.Point.X, cell.Point.Y);
        }
    });

    setTimeout(function () {
        modCells.forEach(cell => {
            if (cell.Destroy) {
                const deleteCell = document.getElementById(getCellId(cell.Id));

                if (deleteCell) {
                    deleteCell.remove();
                }
            }
        });
    }, 75);
}
