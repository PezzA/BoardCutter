import { Twenty48 } from "./2048/game";
import { WindowElements } from "./2048/WindowElements"

var twenty48Game: Twenty48 | undefined = undefined;

export function start2048Game(scoreLineElementId: string,
    logContainerElementId: string,
    gameBoardElementId: string,
    newGameElementId: string,
    gameContainerElementId: string,
    gameAreaElementId: string,
    gameOverElementId: string,
    layoutDataElementId: string,
    debuglogElementId: string,
    readyFunc: () => void,
    toastFunc: (message: string) => void): void {

    const windowElements: WindowElements = {
        scoreLine: getElementAndThrow(scoreLineElementId),
        logContainer: getElementAndThrow(logContainerElementId),
        gameBoard: getElementAndThrow(gameBoardElementId),
        newGame: getElementAndThrow(newGameElementId),
        gameArea: getElementAndThrow(gameAreaElementId),
        gameContainer: getElementAndThrow(gameContainerElementId),
        gameOver: getElementAndThrow(gameOverElementId),
        layoutData: getElementAndThrow(layoutDataElementId),
        debugLog: getElementAndThrow(debuglogElementId)
    };

    twenty48Game = new Twenty48(windowElements, readyFunc, toastFunc);
    twenty48Game.setup();
}

function getElementAndThrow(elementId: string): HTMLElement {
    const element = document.getElementById(elementId);

    if (!element) {
        throw new Error(`Could not find element: ${elementId}`);
    }

    return element;
}

interface Twenty48Game {
    initGame: (scoreLineElementId: string,
        logContainerElementId: string,
        gameBoardElementId: string,
        newGameElementId: string,
        gameContainerElementId: string,
        gameAreaElementId: string,
        gameOverElementId: string,
        layoutDataElementId: string,
        debuglogElementId: string,
        readyFunc: () => void,
        toastFunc: (message: string) => void) => void;
    startGame: () => void;
}

declare global {
    interface Window {
        twenty48Game: Twenty48Game;
    }
}

window.twenty48Game = {
    initGame: start2048Game,
    startGame: () => {
        if (!twenty48Game) {
            console.error("Twenty48 Game Not Initialised");
            return;
        }

        twenty48Game.startGame();
    }
}

