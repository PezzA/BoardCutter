import * as signalR from "@microsoft/signalr";
import { Logger } from "../logger";
import { WindowElements } from "./WindowElements";

const GAME_ID_PARAM = "gameid";
const GAME_HUB_URL = "/Twenty48Hub";

export class Twenty48 {
    readonly windowElements: WindowElements;
    private connection: signalR.HubConnection;
    readonly _readyHandler: () => void;
    readonly _toastHandler: (message: string) => void;
    private gameId: string;
    readonly logger: Logger;

    private _xDown: number | null = null;
    private _yDown: number | null = null;

    private _cellWidth = 0;
    private _cellMargin = 0;
    private _prevSize: string = '';
    private _modCells: any = [];
    private _keydownHandler: any;
    private _resizeHanlder: any;

    private addEventListeners(this: Twenty48): void {
        this.removeEventListeners();

        const classClosure: Twenty48 = this;

        this._keydownHandler = function (this: Window, e: KeyboardEvent) {
            if (e.repeat) return;

            if (e.key === "ArrowUp" || e.key === "ArrowDown" || e.key === "ArrowLeft" || e.key === "ArrowRight") {
                e.preventDefault();

                var decodedKey = classClosure.getDirectionFromKey(e.key);

                classClosure.logger.logUp("Move: " + "[GameId:" + classClosure.gameId + "]" + " [Direction:" + decodedKey + "]");

                if (decodedKey !== -1) {
                    classClosure.connection
                        .invoke("Move", classClosure.gameId, decodedKey)
                        .catch(function (err: Error) {
                            console.log("Could not invoke method [Move] on signalR connection." + err.toString());
                        });

                    classClosure.logger.logDebug("Procssing Move: " + decodedKey);
                }
            }
        }

        this._resizeHanlder = function () {
            classClosure.resize();
        }

        // https://stackoverflow.com/questions/2264072/detect-a-finger-swipe-through-javascript-on-the-iphone-and-android
        //this.windowElements.gameBoard.addEventListener('touchstart', this.handleTouchStart);
        //this.windowElements.gameBoard.addEventListener('touchend', this.handleTouchEnd);
        window.addEventListener("keydown", this._keydownHandler);
        window.addEventListener("resize", this._resizeHanlder);
    }

    private removeEventListeners(this: Twenty48): void {
        window.removeEventListener("resize", this._resizeHanlder);
        window.removeEventListener("keydown", this._keydownHandler);
    }

    constructor(elements: WindowElements, readyHandler: () => void, toastHandler: (message: string) => void) {
        this.windowElements = elements;
        this._readyHandler = readyHandler;
        this._toastHandler = toastHandler;
        this.logger = new Logger(this.windowElements.logContainer);
    }

    public setup(this: Twenty48): void {
        const urlParams = new URLSearchParams(window.location.search);
        const myParam = urlParams.get(GAME_ID_PARAM);

        if (myParam) {
            this.logger.logDebug("GameId, found, showing screen: " + myParam);
            this.gameId = myParam;
            this.showStart(false, true);
            this.initGrid();
        } else {
            this.logger.logDebug("No GameId found. Showing Setup");
            this.showStart(true, false);

        }

        this.connect(myParam);
        // this.resize(true);
    }

    public startGame(this: Twenty48): void {
        this.logger.logUp("StartNew:");

        const that = this;
        this.connection
            .invoke("StartNew")
            .catch(function (err: Error) {
                that.logger.logError(`Could not invoke StartNew on signalR Connection. Error : ${err.message}`);
            });

    }

    connect(this: Twenty48, gameId: string): void {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(GAME_HUB_URL)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.configureHandlers(connection);

        const that = this;

        connection
            .start()
            .then(function () {
                that.logger.logSuccess("SignalR Connection Established");
                that._readyHandler();

                that.logger.logUp(`CheckPlayerStatus: ${gameId ?? ""}`);
                that.connection
                    .invoke("CheckPlayerStatus", gameId ?? "")
                    .catch(function (err) {
                        that.logger.logError("Could not invoke method [] on signalR connection." + err.toString());
                    });
            })
            .catch(function (err) {
                that.logger.logError("could not establish a signalR connection." + err.toString());
            });

        this.connection = connection;
    }

    configureHandlers(this: Twenty48, conn: signalR.HubConnection): signalR.HubConnection {
        const that = this;
        conn.on("PlayerStatus", function (message: string) {
            that.logger.logDown("PlayerStatus: " + message);
        });

        conn.on("ErrorMessage", function (message: string) {
            that.logger.logError("ErrorMessage: " + message);
            that._toastHandler(message);
        });

        conn
            .on("SetPlayerGame", function (message: string) {
                var data = JSON.parse(message);
                that.logger.logDown("SetPlayerGame: " + message);
                window.location.href += `?gameid=` + data.GameId;
            });

        conn.on("PublicVisible", function (message: string) {
            var data = JSON.parse(message);
            that.logger.logDown("PublicVisible: " + message);
            that._modCells = data.Cells;

            that.windowElements.scoreLine.textContent = data.Score;

            that.drawCells();
            that.logger.logDebug('PubVisible: Done');

            if (data.Status === 3) {
                that.logger.logDebug('End of Game');
                //               this.windowElements.gameBoard.removeEventListener('touchstart', this.handleTouchStart);
                //              this.windowElements.gameBoard.removeEventListener('touchend', this.handleTouchEnd);
                //                this.windowElements.gameBoard.style.opacity = "0.3";
                //                window.removeEventListener("keydown", this.handleKeyDown);
                that.removeEventListeners();
                that.windowElements.gameOver.style.opacity = "1";
            }
        });

        return conn;
    }

    showStart(showStart: boolean, showRunning: boolean): void {
        this.windowElements.newGame.hidden = !showStart;
        this.windowElements.gameContainer.hidden = !showRunning;
    }

    toPixels(this: Twenty48, input: number): number {
        return (input * (this._cellWidth + this._cellMargin)) + this._cellMargin;
    }

    moveCell(this: Twenty48, element: HTMLElement, x: number, y: number): void {
        element.style.top = this.toPixels(y) + "px";
        element.style.left = this.toPixels(x) + "px";
    }

    getCellId(id: string): string {
        return `cellId-${id}`;
    }

    addCell(this: Twenty48, id: number, value: number, x: number, y: number) {

        const node = document.createElement("div");

        node.id = this.getCellId(id.toString());

        node.classList.add('cell');
        node.classList.add(`cell-${value}`);

        if (value !== 0) {
            node.innerText = value.toString();
        }

        node.style.zIndex = id.toString();
        node.style.top = this.toPixels(y) + "px";
        node.style.left = this.toPixels(x) + "px";

        const mainElement = this.windowElements.gameBoard;

        if (mainElement) {
            mainElement.appendChild(node);
        } else {
            console.log("NO EXISTO!!");
        }
    }

    withinOne(val: number, test: number): boolean {
        return Math.abs(val - test) < 1;
    }

    resize(this: Twenty48, forceDraw: boolean = false): void {
        var bounds = this.windowElements.gameContainer.getBoundingClientRect();
        const width = bounds.width;

        const cmpCellWidth = this._cellWidth
        
        if (width < 576) {
            this.logger.logDebug('Setting Small');
            this._cellWidth = 85;
            this._cellMargin = 6;
        } else {
            this.logger.logDebug('Setting Medium');
            this._cellWidth = 150;
            this._cellMargin = 13;
        } 
        
        if(cmpCellWidth != this._cellWidth)
        {
            this.windowElements.gameBoard.innerHTML = '';

            for (let x = 0; x < 4; x++) {
                for (let y = 0; y < 4; y++) {
                    this.addCell(0, 0, x, y);
                }
            }
            this.drawCells();
        }
      
    }

    getDirectionFromKey(key: string): number {
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

    handleKeyDown(this: Twenty48, e: KeyboardEvent) {
        if (e.repeat) return;

        if (e.key === "ArrowUp" || e.key === "ArrowDown" || e.key === "ArrowLeft" || e.key === "ArrowRight") {
            e.preventDefault();

            var decodedKey = this.getDirectionFromKey(e.key);

            this.logger.logUp("Move: " + "[GameId:" + this.gameId + "]" + " [Direction:" + decodedKey + "]");

            if (decodedKey !== -1) {
                this.connection
                    .invoke("Move", this.gameId, decodedKey)
                    .catch(function (this: Twenty48, err: Error) {
                        this.logger.logError("Could not invoke method [Move] on signalR connection." + err.toString());
                    });

                this.logger.logDebug("Procssing Move: " + decodedKey);
            }
        }
    }

    initGrid(this: Twenty48) {
        this.logger.logDebug('Initalising Game');
        this.addEventListeners();

        // https://stackoverflow.com/questions/2264072/detect-a-finger-swipe-through-javascript-on-the-iphone-and-android
        //this.windowElements.gameBoard.addEventListener('touchstart', this.handleTouchStart);
        //this.windowElements.gameBoard.addEventListener('touchend', this.handleTouchEnd);

        this.resize(true);
    };

    drawGame(this: Twenty48) {
        for (let x = 0; x < 4; x++) {
            for (let y = 0; y < 4; y++) {
                this.addCell(0, 0, x, y);
            }
        }

        this.drawCells();
    }

    handleTouchStart(evt: TouchEvent): void {
        console.log(evt);
        evt.preventDefault();
        const firstTouch = evt.touches[0];
        this._xDown = firstTouch.clientX;
        this._yDown = firstTouch.clientY;
    };

    handleTouchEnd(this: Twenty48, evt: TouchEvent) {
        console.log(evt);
        evt.preventDefault();
        if (!this._xDown || !this._yDown) {
            return;
        }

        var xUp = evt.changedTouches[0].clientX;
        var yUp = evt.changedTouches[0].clientY;

        var xDiff = this._xDown - xUp;
        var yDiff = this._yDown - yUp;

        let dir = -1;

        if (Math.abs(xDiff) > Math.abs(yDiff)) {
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
            this.connection
                .invoke("Move", this.gameId, dir)
                .catch(function (this: Twenty48, err: Error) {
                    this.logger.logError("Could not invoke method [Move] on signalR connection." + err.toString());
                });

            this.logger.logDebug("Procssing Move: " + dir);
        }

        this._xDown = null;
        this._yDown = null;
    };

    drawCells(this: Twenty48) {


        if (!this._modCells) {
            this.logger.logWarn("drawCells: No cells to draw");
            return;
        }

        this._modCells.forEach((cell: any) => {
            const cellElement = document.getElementById(this.getCellId(cell.Id));

            if (cellElement) {
                this.moveCell(cellElement, cell.Point.X, cell.Point.Y);
            } else {
                this.addCell(cell.Id, cell.Value, cell.Point.X, cell.Point.Y);
            }
        });

        const that = this;
        setTimeout(function (this: Twenty48) {
            that._modCells.forEach((cell: any) => {
                if (cell.Destroy) {
                    const deleteCell = document.getElementById(that.getCellId(cell.Id));

                    if (deleteCell) {
                        deleteCell.remove();
                    }
                }
            });
        }, 75);
    }
}
