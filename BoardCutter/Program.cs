using BoardCutter.Components;
using BoardCutter.Components.Account;
using BoardCutter.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using BoardCutter.Hubs;
using BoardCutter.Games.Twenty48.Server;
using Akka.Hosting;
using BoardCutter.Core.Actors;
using Akka.Actor;
using Microsoft.AspNetCore.SignalR;
using BoardCutter.Core.Actors.HubWriter;
using BoardCutter.Core.Players;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

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


var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BoardCutter.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapHub<ChatHub>("/chathub");
app.MapHub<Twenty48Hub>("/twenty48hub");

app.Run();
