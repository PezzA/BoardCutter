var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BoardCutter_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");
builder.Build().Run();
