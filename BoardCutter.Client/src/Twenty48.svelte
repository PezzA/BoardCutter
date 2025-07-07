<script lang="ts">
    import {onMount} from 'svelte';
    import * as signalR from '@microsoft/signalr';
    // @ts-ignore - Svelte component import
    import LoggerPanel from './lib/Logger.svelte';
    import Board from "./lib/Board.svelte";

    // Using regular variables in Svelte 5
    let connection: signalR.HubConnection | null = null;
    let connectionStatus: 'disconnected' | 'reconnecting' | 'connected' | 'error' = 'disconnected';
    let cells: number[][] = [];
    let score: number = 0;
    let status = 0;
    let gameId: string = '';

    let loggerPanelRef: any = null;
  
    function pushLog(text: string, cssClass: string) {
        if (loggerPanelRef && loggerPanelRef.addMessage) {
            loggerPanelRef.addMessage({text, cssClass});
        }
    }

    async function connectToSignalR(): Promise<void> {

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/twenty48hub', {
                withCredentials: true
            })
            .withAutomaticReconnect()
            .build();

        connection.onclose(() => connectionStatus = 'disconnected');
        connection.onreconnecting(() => connectionStatus = 'reconnecting');
        connection.onreconnected(() => connectionStatus = 'connected');

        connection.on('SetPlayerGame', (message: any) => {
            try {
                const data = typeof message === 'string' ? JSON.parse(message) : message;
                if (data && data.GameId) {
                    const url = new URL(window.location.href);
                    url.searchParams.set('gameid', data.GameId);
                    window.location.href = url.toString();
                }
            } catch (err) {
                pushLog('Failed to handle SetPlayerGame: ' + err, 'log-error');
            }
        });

        connection.on('PublicVisible', (message: any) => {
            try {
                const data = JSON.parse(message);
                pushLog("PublicVisible: " + message, 'log-down');

                cells = data.Cells;
                
                score = data.Score;
                
                status = data.Status;
                gameId = data.GameId;
   
                pushLog('PubVisible: Done', 'log-debug');

                if (data.Status === 3) {
                    pushLog('End of Game', "log-debug");

                    //that.windowElements.gameBoard.style.opacity = "0.3";
                    //that.removeEventListeners();
                    //that.windowElements.gameOver.style.opacity = "1";
                }
            } catch (err) {
                pushLog('Failed to handle PublicVisible: ' + err, 'log-error');
            }
        });

        try {
            await connection.start();
            connectionStatus = 'connected';

            // Get gameid from query string if it exists, else empty string
            const urlParams = new URLSearchParams(window.location.search);
            const gameId = urlParams.get('gameid') || '';

            try {
                await connection.invoke('CheckPlayerStatus', gameId);
                pushLog('CheckPlayerStatus called with gameId: ' + gameId, 'log-up');
            } catch (err) {
                pushLog('Failed to call CheckPlayerStatus: ' + err, 'log-error');
            }
        } catch (err) {
            connectionStatus = 'error';
            pushLog('SignalR connection error: ' + err, 'log-error');
        }
    }

    async function startNewGame(): Promise<void> {

        if (connectionStatus === 'connected' && connection) {
            try {
                await connection.invoke('StartNew');
            } catch (err) {
                console.error('Failed to start new game:', err);
            }
        }
    }

    onMount(() => {
        setTimeout(() => {
            connectToSignalR();
        }, 200);
    });


</script>

<style>
    :global(body) {
        margin: 0;
        min-height: 100vh;
        box-sizing: border-box;
        border: none;
    }

    :global(.signalr-top-border) {
        border-top: 8px solid #ffb300; /* default amber */
        transition: border-color 0.3s;
    }

    :global(.signalr-top-border.connected) {
        border-top-color: #4caf50; /* green */
    }

    :global(.signalr-top-border.not-connected) {
        border-top-color: #ffb300; /* amber */
    }

    main {
        padding: 1rem;
        font-family: Arial, sans-serif;
        max-width: 800px;
        margin: 2em auto;
        border-radius: 10px;
        border-color: red;
        border-width: 10px;
    }

    .status {
        margin-bottom: 1em;
    }

    .spinner {
        display: inline-block;
        width: 16px;
        height: 16px;
        border: 2px solid #ccc;
        border-top: 2px solid #333;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-right: 8px;
        vertical-align: middle;
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }
        100% {
            transform: rotate(360deg);
        }
    }

</style>

<main class="game-window ">
    <div class="status">SignalR status: {connectionStatus}</div>

    {#if status === 0}
        {#if connectionStatus === 'connected'}
            <button on:click={startNewGame} style="min-width:150px">Start new Game!!!</button>
        {/if}
        
    {:else}
        <Board cells={cells} score={score} gameId={gameId} connection={connection} />
    {/if}

    <LoggerPanel bind:this={loggerPanelRef}/>
</main>
