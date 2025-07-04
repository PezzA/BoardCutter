import * as signalR from "@microsoft/signalr";
import { Logger } from "../logger";
import { WindowElements } from "./WindowElements";


export class Twenty48 {


    readonly logger: Logger;

    private _xDown: number | null = null;
    private _yDown: number | null = null;



    private _resizeHandler: any;
    private _touchStartHandler: any;
    private _touchEndHandler: any;

    private addEventListeners(this: Twenty48): void {
        this.removeEventListeners();
        
        this._touchEndHandler = function (evt: TouchEvent) {
            evt.preventDefault();
            
            if (!classClosure._xDown || !classClosure._yDown) {
                return;
            }
            
            var xUp = evt.changedTouches[0].clientX;
            var yUp = evt.changedTouches[0].clientY;

            var xDiff = classClosure._xDown - xUp;
            var yDiff = classClosure._yDown - yUp;

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
                classClosure.connection
                    .invoke("Move", classClosure.gameId, dir)
                    .catch(function (this: Twenty48, err: Error) {
                        this.logger.logError("Could not invoke method [Move] on signalR connection." + err.toString());
                    });

                classClosure.logger.logDebug("Processing Move: " + dir);
            }

            classClosure._xDown = null;
            classClosure._yDown = null;
        }

        this._touchStartHandler = function (evt: TouchEvent) {
            evt.preventDefault();
            const firstTouch = evt.touches[0];
            classClosure._xDown = firstTouch.clientX;
            classClosure._yDown = firstTouch.clientY;
        }
        
        this._resizeHandler = function () {
            classClosure.resize();
        }

        // https://stackoverflow.com/questions/2264072/detect-a-finger-swipe-through-javascript-on-the-iphone-and-android
        this.windowElements.gameBoard.addEventListener('touchstart', this._touchStartHandler);
        this.windowElements.gameBoard.addEventListener('touchend', this._touchEndHandler);
        window.addEventListener("resize", this._resizeHandler);
    }

    private removeEventListeners(this: Twenty48): void {
        window.removeEventListener("resize", this._resizeHandler);
        this.windowElements.gameBoard.removeEventListener('touchstart', this._touchStartHandler);
        this.windowElements.gameBoard.removeEventListener('touchend', this._touchEndHandler);
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
    }
}
