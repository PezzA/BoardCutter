var builder = DistributedApplication.CreateBuilder(args);

var boardCutterWeb = builder.AddProject<Projects.BoardCutter_Web>("BoardCutter-Web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddNpmApp("BoardCutter-Vite-Dev", "../BoardCutter.Client", "dev")
    .WaitFor(boardCutterWeb);

builder.Build().Run();
