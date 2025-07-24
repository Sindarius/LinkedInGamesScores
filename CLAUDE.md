# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Application Architecture

This is a full-stack LinkedIn Game Scores tracking application with these components:

- **Frontend**: Vue.js 3 SPA using PrimeVue components and TailwindCSS (`game.client/`)
- **Backend**: .NET 8 WebAPI with Entity Framework Core (`game.api/`)
- **Database**: PostgreSQL with persistent data storage
- **Containerization**: Docker Compose orchestration

The application tracks daily scores from LinkedIn games (Queens, Pinpoint, Crossclimb) with leaderboards and historical data viewing.

## Development Commands

### Client (Vue.js) - `game.client/`
```bash
npm install          # Install dependencies
npm run dev          # Start development server (Vite)
npm run build        # Production build
npm run preview      # Preview production build
npm run lint         # ESLint with auto-fix
```

### API (.NET 8) - `game.api/`
```bash
dotnet restore       # Restore NuGet packages
dotnet run           # Start development server
dotnet build         # Build project
dotnet ef migrations add <name>    # Create migration
dotnet ef database update          # Apply migrations
```

### Docker Environment
```bash
# Setup (run once)
setup.bat            # Windows setup script
./setup.sh           # Linux/Mac setup script

# Container management
docker-compose up -d              # Start all services
docker-compose up -d --build      # Rebuild and start
docker-compose logs -f            # View logs
docker-compose down               # Stop services
docker-compose down -v            # Stop and remove volumes (deletes data)
```

- Do not use version in docker files

## Architecture Details

### Frontend Architecture (`game.client/`)
- **Framework**: Vue 3 with Composition API
- **UI Library**: PrimeVue 4.3.1 with auto-import resolver
- **Styling**: TailwindCSS + PrimeUI theming
- **Build Tool**: Vite 5.3.1
- **State Management**: Local component state (no Vuex/Pinia)
- **API Integration**: Custom GameService class with fetch API

**Key Components**:
- `GameTabs.vue` - Main tabbed interface for games
- `DailyLeaderboard.vue` - Leaderboard display with date navigation
- `ScoreSubmissionForm.vue` - Score submission form
- `gameService.js` - API communication layer

### Backend Architecture (`game.api/`)
- **Framework**: .NET 8 WebAPI
- **ORM**: Entity Framework Core 9.0.7
- **Database**: PostgreSQL (production) / In-Memory (development option)
- **API Documentation**: Swagger/OpenAPI
- **CORS**: Configured for Vue.js client integration

**Data Models**:
- `Game.cs` - Game definition (Queens, Pinpoint, Crossclimb)
- `GameScore.cs` - Individual score records
- `GameContext.cs` - EF DbContext with seed data

### API Environment Detection
The Vue client automatically detects environment:
- **Development**: `https://localhost:7036/api` (when running API locally)
- **Docker/Production**: `http://localhost:5000/api`

### Database Schema
- **Games**: Pre-seeded with 3 LinkedIn games
- **GameScores**: Player scores with foreign key to Games
- **Migrations**: Auto-applied on API startup

## Service URLs

- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **API Docs**: http://localhost:5000/swagger
- **PostgreSQL**: localhost:5432 (user: postgres, password: postgres, db: gamescores)

## Data Persistence

PostgreSQL data persists in `./data/postgres/` directory (bind mount). Don't delete this directory unless you want to lose all game scores.

## Common Development Patterns

1. **API Changes**: Modify models, create migrations, update controllers
2. **Frontend Changes**: Components are auto-imported via unplugin-vue-components
3. **Styling**: Uses TailwindCSS classes with PrimeVue component styling
4. **Testing**: No test framework currently configured
5. **Linting**: ESLint with Prettier integration for Vue.js client only

## Project Structure Notes

- Client uses PrimeVue's Sakai template structure
- API follows standard .NET WebAPI patterns with Controllers/Models/Data separation
- Docker setup uses multi-stage builds for both client and API
- No authentication/authorization implemented
- CORS configured to allow frontend-backend communication

## Coding Instructions
- Prefer simple solutions over complex solutions
- Take time to forumlate a solution to a problem over trying many approaches where possible
- Use SOLID design principals where applicable
- Break logical chunks of code such as Web API and Entity Framework classes into separate projects for maintainability