  async function connectToLobby() {
    try {
      connection = new HubConnectionBuilder()
        .withUrl("/gamelobbyhub", {
          withCredentials: true
        })
        .build();

