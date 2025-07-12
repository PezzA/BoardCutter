<script>
    // Use standard reactive variables for compatibility

    export let message = null;
    let messages = [];
    let expanded = true;

    function toggle() {
        expanded = !expanded;
    }

    function clearLogs() {
        messages = [];
    }

    export function addMessage(msg) {
        messages = [...messages, msg];
    }

    $: if (message) {
        addMessage(message);
    }
</script>

<!-- Bootstrap Accordion -->
<div class="accordion mb-3" id="loggerAccordion">
    <div class="accordion-item">
        <h2 class="accordion-header" id="loggerHeading">
            <button
                    class="accordion-button{expanded ? '' : ' collapsed'}"
                    type="button"
                    aria-expanded={expanded}
                    aria-controls="loggerCollapse"
                    on:click={toggle}
                    on:keydown={(e) => { if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); } }}
            >
                Logs
            </button>
        </h2>
        <div id="loggerCollapse" class="accordion-collapse collapse{expanded ? ' show' : ''}" aria-labelledby="loggerHeading" data-bs-parent="#loggerAccordion">
            <div class="accordion-body" style="max-height:120px;overflow-y:auto;">
                <div class="d-flex align-items-center mb-2">

                    <button class="btn btn-sm btn-outline-secondary ms-2" on:click={clearLogs} aria-label="Clear logs" type="button">Clear</button>
                </div>
                {#each messages as msg}
                    <div class="log-entry {msg.cssClass}">{msg.text}</div>
                {/each}
            </div>
        </div>
    </div>
</div>

<style>
    
    .accordion-body{
        background-color: #1a1d20;
        height:1000px;
    }
    
    .log-entry {
        padding: 0.25rem;
        border-radius: 0.25rem;
        margin-bottom: 0.25rem;
        font-size: 0.675rem;
        font-family: monospace;
    }
    .log-success { color: #ffc720; }
    .log-error { color: #f44336; }
    .log-warn { color: #ffc720; background-color: #000000; }
    .log-debug { color: #2196f3; }
    .log-up { color: red; }
    .log-down { color: #ffb300; }
</style>
