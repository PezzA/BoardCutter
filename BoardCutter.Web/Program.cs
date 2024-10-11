using Akka.Actor;
using Akka.Hosting;

using BoardCutter.Core.Actors;
using BoardCutter.Core.Actors.HubWriter;
using BoardCutter.Core.Players;
using BoardCutter.Games.Twenty48.Server;

using Microsoft.AspNetCore.SignalR;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

builder.Services.AddSingleton<IPlayerService, MemoryPlayerService>();

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithActors((system, registry, resolver) =>
        {
            var twenty48HubWriter =
                system.ActorOf(
                    Props.Create(() =>
                        new HubClientWriter<Twenty48Hub>(
                            resolver.GetService<IHubContext<Twenty48Hub>>(),
                            resolver.GetService<IPlayerService>())),
                    "2048HubWriter");

            var gameActors = new Dictionary<string, Props>
            {
                {
                    "2048",
                    Props.Create(() =>  new BoardCutter.Games.Twenty48.Server.Actors.GameActor(twenty48HubWriter, new RandomTilePlacer()))
                }
            };

            var gameManagerActor =
                system.ActorOf(
                    Props.Create(
                        () => new GameManager(gameActors)),

                    "GameManagerActor");

            registry.Register<GameManager>(gameManagerActor);
        });
});


WebApplication app = builder.Build();

await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });
await app.RunAsync();
