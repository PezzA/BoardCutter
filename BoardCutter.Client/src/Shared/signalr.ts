import * as signalR from "@microsoft/signalr";

export type ConnectionStatus = "disconnected" | "reconnecting" | "connected" | "error";

export interface SignalRConnectionOptions {
    hubUrl: string;
    withCredentials?: boolean;
    automaticReconnect?: boolean;
}

export interface SignalRConnection {
    connection: signalR.HubConnection | null;
    connectionStatus: ConnectionStatus;
    connect: () => Promise<void>;
    disconnect: () => Promise<void>;
    invoke: (methodName: string, ...args: any[]) => Promise<any>;
    on: (methodName: string, callback: (...args: any[]) => void) => void;
    off: (methodName: string, callback?: (...args: any[]) => void) => void;
    onStatusChange: (callback: (status: ConnectionStatus) => void) => () => void;
}

export function createSignalRConnection(options: SignalRConnectionOptions): SignalRConnection {
    let connection: signalR.HubConnection | null = null;
    let connectionStatus: ConnectionStatus = "disconnected";
    let statusCallbacks: Array<(status: ConnectionStatus) => void> = [];
    let pendingEventHandlers: Array<{ methodName: string; callback: (...args: any[]) => void }> = [];

    function updateStatus(newStatus: ConnectionStatus) {
        connectionStatus = newStatus;
        statusCallbacks.forEach(callback => callback(connectionStatus));
    }

    function onStatusChange(callback: (status: ConnectionStatus) => void) {
        statusCallbacks.push(callback);
        // Return unsubscribe function
        return () => {
            statusCallbacks = statusCallbacks.filter(cb => cb !== callback);
        };
    }

    async function connect(): Promise<void> {
        if (connection) {
            console.warn("SignalR connection already exists");
            return;
        }

        try {
            updateStatus("reconnecting");

            const builder = new signalR.HubConnectionBuilder()
                .withUrl(options.hubUrl, {
                    withCredentials: options.withCredentials ?? true,
                });

            if (options.automaticReconnect !== false) {
                builder.withAutomaticReconnect();
            }

            connection = builder.build();

            // Set up any pending event handlers
            pendingEventHandlers.forEach(({ methodName, callback }) => {
                connection!.on(methodName, callback);
            });

            // Set up connection event handlers
            connection.onclose(() => {
                updateStatus("disconnected");
                console.log(`SignalR connection to ${options.hubUrl} closed`);
            });

            connection.onreconnecting(() => {
                updateStatus("reconnecting");
                console.log(`SignalR connection to ${options.hubUrl} reconnecting...`);
            });

            connection.onreconnected(() => {
                updateStatus("connected");
                console.log(`SignalR connection to ${options.hubUrl} reconnected`);
            });

            // Start the connection
            await connection.start();
            updateStatus("connected");
            console.log(`Connected to SignalR hub: ${options.hubUrl}`);
        } catch (err) {
            updateStatus("error");
            console.error(`SignalR connection error for ${options.hubUrl}:`, err);
            throw err;
        }
    }

    async function disconnect(): Promise<void> {
        if (connection) {
            try {
                await connection.stop();
                console.log(`SignalR connection to ${options.hubUrl} stopped`);
            } catch (err) {
                console.error(`Error stopping SignalR connection to ${options.hubUrl}:`, err);
            } finally {
                connection = null;
                pendingEventHandlers = []; // Clear pending handlers
                updateStatus("disconnected");
            }
        }
    }

    async function invoke(methodName: string, ...args: any[]): Promise<any> {
        if (!connection) {
            throw new Error("SignalR connection not established");
        }

        if (connectionStatus !== "connected") {
            throw new Error(`Cannot invoke method '${methodName}': connection status is '${connectionStatus}'`);
        }

        try {
            return await connection.invoke(methodName, ...args);
        } catch (err) {
            console.error(`Failed to invoke SignalR method '${methodName}':`, err);
            throw err;
        }
    }

    function on(methodName: string, callback: (...args: any[]) => void): void {
        if (connection) {
            // Connection exists, register immediately
            connection.on(methodName, callback);
        } else {
            // Store for later registration when connection is established
            pendingEventHandlers.push({ methodName, callback });
        }
    }

    function off(methodName: string, callback?: (...args: any[]) => void): void {
        if (connection) {
            if (callback) {
                connection.off(methodName, callback);
            } else {
                connection.off(methodName);
            }
        } else {
            // Remove from pending handlers if not yet connected
            if (callback) {
                pendingEventHandlers = pendingEventHandlers.filter(
                    handler => !(handler.methodName === methodName && handler.callback === callback)
                );
            } else {
                pendingEventHandlers = pendingEventHandlers.filter(
                    handler => handler.methodName !== methodName
                );
            }
        }
    }

    return {
        get connection() { return connection; },
        get connectionStatus() { return connectionStatus; },
        connect,
        disconnect,
        invoke,
        on,
        off,
        onStatusChange
    };
}

// Utility function to create a reactive store for Svelte
export function createReactiveSignalRConnection(options: SignalRConnectionOptions) {
    const baseConnection = createSignalRConnection(options);

    return {
        get connection() { return baseConnection.connection; },
        get connectionStatus() { return baseConnection.connectionStatus; },
        connect: baseConnection.connect,
        disconnect: baseConnection.disconnect,
        invoke: baseConnection.invoke,
        on: baseConnection.on,
        off: baseConnection.off,
        onStatusChange: baseConnection.onStatusChange
    };
}