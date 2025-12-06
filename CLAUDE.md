# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BoardCutter is a .NET-based multiplayer game server built with .NET Aspire, ASP.NET Core, SignalR, Akka.NET actors, and a Svelte 5 frontend. It provides a framework for creating and hosting turn-based board games with real-time communication.

## Architecture

### Core Components

**Aspire Orchestration (BoardCutter.AppHost)**
- Entry point for development: runs both the ASP.NET backend and Vite dev server
- Located in `BoardCutter.AppHost/AppHost.cs`
- Manages service orchestration via .NET Aspire

**Backend (BoardCutter.Web)**
- ASP.NET Core application serving SignalR hubs and static files
- `Program.cs` configures authentication (Auth0 + anonymous cookie-based), CORS, Akka.NET actors, and SignalR
- In development, `SvelteDevProxyMiddleware` proxies non-API requests to Vite dev server (localhost:5173)
- In production, serves static files from `BoardCutter.Client/dist`

**Actor System (BoardCutter.Core.Actors)**
- Uses Akka.NET for game state management
- `GameManager`: Central coordinator that creates/manages game instances and maintains game registry
- `HubClientWriter<T>`: Actor that sends messages to SignalR clients (bridges actors and SignalR)
- Messages flow: SignalR Hub → GameManager → Game-specific Actor → HubClientWriter → SignalR clients

**Game Implementation (BoardCutter.Games.Twenty48)**
- Game-specific actors inherit from Akka.NET's `ReceiveActor`
- `GameActor`: Implements 2048 game logic (grid state, moves, scoring, win/loss detection)
- Each game type needs its own actor and SignalR hub
- Game actors communicate with clients via `HubClientWriter` actor

**Frontend (BoardCutter.Client)**
- Svelte 5 application using Vite
- `src/main.ts`: Routes to different game components based on URL path
- SignalR connection handled in individual game components
- Component structure: `src/Shared/` for common UI, `src/Twenty48/` for game-specific components

### Communication Flow

1. Client connects to SignalR hub (e.g., `Twenty48Hub`)
2. Hub methods forward messages to `GameManager` actor
3. `GameManager` routes to appropriate game actor or creates new game instance
4. Game actor processes logic and sends updates via `HubClientWriter`
5. `HubClientWriter` sends JSON-serialized messages back to SignalR clients

### Authentication

- Dual authentication: Auth0 (registered users) and BoardCutter cookies (anonymous users)
- `BoardCutterCookieMiddleware` manages anonymous user sessions
- `IPlayerService` (in-memory) tracks player connections and maps to SignalR connection IDs

## Development Commands

### Running the Application

```bash
# Run the entire application (backend + frontend) via Aspire
dotnet run --project BoardCutter.AppHost

# Frontend only (from BoardCutter.Client/)
npm run dev

# Backend only
dotnet run --project BoardCutter.Web
```

The Aspire orchestrator is the recommended way to run the full stack in development.

### Building

```bash
# Build entire solution
dotnet build

# Build frontend
cd BoardCutter.Client
npm run build

# Build specific project
dotnet build BoardCutter.Web
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test BoardCutter.Games.Twenty48.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

Tests use xUnit and Akka.TestKit for actor testing.

### Frontend

```bash
# Type checking
npm run check

# Preview production build
npm run preview
```

## Project Structure

- `BoardCutter.AppHost/` - .NET Aspire orchestrator
- `BoardCutter.Web/` - ASP.NET Core application (SignalR hubs, middleware, authentication)
- `BoardCutter.Client/` - Svelte 5 frontend (TypeScript, Vite)
- `BoardCutter.Core/` - Core domain types (Player, GameStatus, Point2D)
- `BoardCutter.Core.Actors/` - Akka.NET actors and messages
- `BoardCutter.Games.Twenty48/` - 2048 game implementation
- `BoardCutter.Games.Twenty48.Tests/` - Game logic tests
- `BoardCutter.ServiceDefaults/` - Shared Aspire configuration

## Key Patterns

**Adding a New Game**
1. Create new project `BoardCutter.Games.[GameName]`
2. Implement game-specific actor inheriting from `ReceiveActor`
3. Create SignalR hub in `BoardCutter.Web/Hubs/`
4. Register actor with `GameManager` in `Program.cs`
5. Create `HubClientWriter<YourHub>` actor for client communication
6. Add Svelte component in `BoardCutter.Client/src/[GameName]/`
7. Update `main.ts` routing logic

**Actor Message Pattern**
- Messages defined in `*Messages.cs` files as records
- Notifications for game state changes in `*Notifications.cs`
- Actor receives messages via `Receive<TMessage>()` in constructor
- Use `Tell()` for fire-and-forget, `Ask()` for request-response

**SignalR Communication**
- Hub methods receive player context from SignalR connection
- Messages sent to actors include `Player` object
- Responses go through `HubClientWriter` which serializes to JSON
- Client-side uses `@microsoft/signalr` package

## Technology Stack

- .NET 9.0
- ASP.NET Core (SignalR, Razor Pages)
- .NET Aspire (orchestration)
- Akka.NET 1.5.x (actor framework)
- Svelte 5 (frontend)
- TypeScript 5.8
- Vite 6 (build tool)
- xUnit (testing)
- Auth0 (authentication)

## Development Notes

- CORS configured for `localhost:5173` in development
- Cookie authentication allows anonymous gameplay without registration
- SignalR connection IDs managed by `IPlayerService`
- Actor system named "MyActorSystem" configured in `Program.cs`
- Frontend proxied in dev mode, served as static files in production
