# LinkedIn Game Scores API

A .NET 8 WebAPI for managing LinkedIn game scores including Queens, Pinpoint, and Crossclimb.

## Features

- **Games Management**: CRUD operations for game types
- **Score Tracking**: Submit and retrieve game scores
- **Leaderboards**: Get top scores for each game
- **CORS Support**: Configured for Vue.js client integration

## API Endpoints

### Games
- `GET /api/games` - Get all active games
- `GET /api/games/{id}` - Get specific game with scores
- `POST /api/games` - Create new game
- `PUT /api/games/{id}` - Update game
- `DELETE /api/games/{id}` - Soft delete game

### Game Scores
- `GET /api/gamescores` - Get all scores
- `GET /api/gamescores/{id}` - Get specific score
- `GET /api/gamescores/game/{gameId}` - Get scores for specific game
- `GET /api/gamescores/game/{gameId}/leaderboard?top=10` - Get top scores
- `POST /api/gamescores` - Submit new score
- `PUT /api/gamescores/{id}` - Update score
- `DELETE /api/gamescores/{id}` - Delete score

## Quick Start

```bash
cd game.api
dotnet run
```

Navigate to `https://localhost:7xxx/swagger` for API documentation.

## Pre-seeded Games

1. **Queens** - LinkedIn Queens puzzle game
2. **Pinpoint** - LinkedIn Pinpoint geography game  
3. **Crossclimb** - LinkedIn Crossclimb word ladder game

## Technologies

- .NET 8 WebAPI
- Entity Framework Core with In-Memory Database
- Swagger/OpenAPI documentation
- CORS enabled for frontend integration