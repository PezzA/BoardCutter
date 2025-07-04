using Akka.Actor;
using Akka.Hosting;

using Auth0.AspNetCore.Authentication;

using BoardCutter.Core.Actors;
using BoardCutter.Core.Actors.HubWriter;
using BoardCutter.Core.Players;
using BoardCutter.Games.Twenty48;
using BoardCutter.Web.Extensions;
using BoardCutter.Web.Middleware;

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

// Add BoardCutter cookie-based authentication for anonymous users
builder.Services.AddBoardCutterAuthentication();

// Configure authorization policies to support multiple authentication schemes
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Auth0", "BoardCutter")
        .Build();
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


// Add CORS policy to allow localhost:5174 and share cookies
builder.Services.AddCors(options =>
{
    options.AddPolicy("Localhost5173Policy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRazorPages();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.None; // Do not set Secure on cookies
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Domain = "localhost";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Do not set Secure on cookies
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();

// Use CORS policy before routing
app.UseCors("Localhost5173Policy");

// Add BoardCutter cookie middleware
app.UseMiddleware<BoardCutterCookieMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<Twenty48Hub>("/twenty48hub");
app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
