using Akka.Actor;
using Akka.Hosting;

using Auth0.AspNetCore.Authentication;

using BoardCutter.Core.Actors;
using BoardCutter.Core.Actors.HubWriter;
using BoardCutter.Core.Players;
using BoardCutter.Games.Twenty48;
using BoardCutter.Web.Hubs;

using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
});

builder.Services.AddSingleton<IPlayerService, MemoryPlayerService>();

builder.Services.AddAkka("MyActorSystem", configurationBuilder => configurationBuilder
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
                Props.Create(() =>  new GameActor(twenty48HubWriter, new RandomTilePlacer()))
            }
        };
        
        var gameManagerActor =
            system.ActorOf(
                Props.Create(
                    () => new GameManager(gameActors)),
                "GameManagerActor");
        
        registry.Register<GameManager>(gameManagerActor);
    }));


builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/chat");
app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
