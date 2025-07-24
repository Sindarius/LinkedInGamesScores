# LinkedIn Game Scores

A full-stack application for tracking daily scores from LinkedIn games (Queens, Pinpoint, Crossclimb) with leaderboards and historical data viewing.

## Architecture

- **Frontend**: Vue.js 3 with PrimeVue components
- **Backend**: .NET 8 WebAPI with Entity Framework Core
- **Database**: PostgreSQL with persistent data storage
- **Containerization**: Docker & Docker Compose

## Quick Start with Docker

### Prerequisites
- Docker and Docker Compose installed
- Git

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd LinkedInGameScores
   ```

2. **Start all services**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - **Frontend**: http://localhost:3000
   - **API**: http://localhost:5000
   - **API Documentation**: http://localhost:5000/swagger

### Services

- **PostgreSQL Database**: `localhost:5432`
  - Database: `gamescores`
  - Username: `postgres` 
  - Password: `postgres`
  - **Persistent Data**: Stored in `./data/postgres/`

- **API Server**: `localhost:5000`
  - .NET 8 WebAPI
  - Entity Framework with PostgreSQL
  - Swagger documentation enabled

- **Client Application**: `localhost:3000`
  - Vue.js SPA served by Nginx
  - Responsive design with PrimeVue

## Features

### Score Submission
- Submit daily scores for LinkedIn games
- Player name and optional LinkedIn profile URL
- Form validation and success feedback

### Daily Leaderboards  
- View current day's top scores
- Historical data navigation with date picker
- Tabbed interface for individual games or combined view
- Player rankings with badges for top 3

### Statistics Dashboard
- Total scores submitted
- Active player count
- Available games overview

## Development

### Local Development (without Docker)

1. **Database Setup**
   ```bash
   # Start PostgreSQL (using Docker)
   docker run --name postgres-dev -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
   ```

2. **API Development**
   ```bash
   cd game.api
   dotnet restore
   dotnet run
   ```

3. **Client Development**
   ```bash
   cd game.client  
   npm install
   npm run dev
   ```

### Docker Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Rebuild and restart
docker-compose up -d --build

# Remove all containers and volumes (CAUTION: Deletes data)
docker-compose down -v
```

### Database Migrations

The API automatically handles database migrations on startup. For manual migration:

```bash
cd game.api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## API Endpoints

### Games
- `GET /api/games` - Get all active games
- `POST /api/games` - Create new game
- `PUT /api/games/{id}` - Update game
- `DELETE /api/games/{id}` - Soft delete game

### Game Scores  
- `GET /api/gamescores` - Get all scores
- `GET /api/gamescores/game/{gameId}` - Get scores for specific game
- `GET /api/gamescores/game/{gameId}/leaderboard?top=10` - Get leaderboard
- `POST /api/gamescores` - Submit new score
- `PUT /api/gamescores/{id}` - Update score
- `DELETE /api/gamescores/{id}` - Delete score

## Data Persistence

PostgreSQL data is persisted in the `./data/postgres/` directory on your host machine. This ensures data survives container restarts and removals.

**⚠️ Important**: Don't delete the `./data/` directory unless you want to lose all game scores!

## Configuration

### Environment Variables

**API Container**:
- `ASPNETCORE_ENVIRONMENT`: Set to `Production` 
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string

**Database Container**:
- `POSTGRES_DB`: Database name (`gamescores`)
- `POSTGRES_USER`: Database user (`postgres`)
- `POSTGRES_PASSWORD`: Database password (`postgres`)

### Client Configuration

The Vue.js client automatically detects the environment:
- **Development**: Uses `https://localhost:7036/api`
- **Production**: Uses `http://localhost:5000/api`

## Troubleshooting

### Database Connection Issues
```bash
# Check if PostgreSQL is running
docker-compose ps postgres

# View database logs
docker-compose logs postgres

# Recreate database (will lose data)
docker-compose down postgres
docker volume rm linkedingamescores_postgres_data
docker-compose up -d postgres
```

### API Issues
```bash
# View API logs
docker-compose logs api

# Restart API only
docker-compose restart api
```

### Client Issues
```bash
# View client logs  
docker-compose logs client

# Rebuild client
docker-compose up -d --build client
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with Docker Compose
5. Submit a pull request

## License

This project is licensed under the MIT License.