export class Logger {
    readonly elem: HTMLElement;
    constructor(logElement: HTMLElement) {
        this.elem = logElement;
    }

    logText(this: Logger, text: string, level: string): void {
        var logElement = document.createElement('p');
        logElement.textContent = new Date().toLocaleTimeString() + ': ' + text;
        logElement.classList.add('log' + level);
        this.elem.appendChild(logElement);
    };

    logUp(text: string) {
        this.logText(text, "Up");
    }

    logDown(text: string) {
        this.logText(text, "Down");
    }

    logSuccess(text: string) {
        this.logText(text, "Success");
    }

    logInfo(text: string) {
        this.logText(text, "Info");
    }

    logError(text: string) {
        this.logText(text, "Error");
    }

    logWarn(text: string) {
        this.logText(text, "Warn");
    }

    logDebug(text: string) {
        this.logText(text, "Debug");
    }
}
