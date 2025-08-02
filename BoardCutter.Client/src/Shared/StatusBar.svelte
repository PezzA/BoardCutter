<script lang="ts">
  interface Props {
    connectionStatus: "disconnected" | "reconnecting" | "connected" | "error";
  }

  let { connectionStatus }: Props = $props();
</script>

<div
  class="status-bar"
  class:connected={connectionStatus === "connected"}
  class:reconnecting={connectionStatus === "reconnecting"}
  class:disconnected={connectionStatus === "disconnected" ||
    connectionStatus === "error"}
></div>

<style>
  .status-bar {
    height: 4px;
    width: 100vw;
    position: fixed;
    top: 0;
    left: 0;
    z-index: 1000;
    transition: background-color 0.3s ease;
  }

  .status-bar.connected {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #4caf50 60px,
      #4caf50 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }

  .status-bar.reconnecting {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #ffb300 60px,
      #ffb300 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }

  .status-bar.disconnected {
    background: linear-gradient(
      to right,
      rgba(255, 255, 255, 1) 0%,
      #f44336 60px,
      #f44336 calc(100% - 60px),
      rgba(255, 255, 255, 1) 100%
    );
  }
</style>
