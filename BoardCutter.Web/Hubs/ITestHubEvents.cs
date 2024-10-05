public interface ITestHubEvents
{
    // Define the events the clients can listen to
    public Task Pong(string message);
}
