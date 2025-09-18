# Repository Guidelines

## Project Structure
- `game.api/` – .NET 8 WebAPI (EF Core, Swagger). Controllers in `Controllers/`, models in `Models/`, migrations in `Migrations/`.
- `game.client/` – Vue 3 + Vite + PrimeVue. App code in `src/`, assets in `public/`, e2e tests in `tests/`.
- `docker-compose.yml` – Dev stack (Postgres + API). Data persisted under `data/postgres/`. Example envs in `.env.example`.

## Build, Test & Development
- API: `cd game.api && dotnet restore && dotnet run` (dev at `http://localhost:5220`, Swagger at `/swagger`).
- Client: `cd game.client && npm install && npm run dev` (Vite dev server).
- Docker: `docker-compose up -d` (start DB+API), `docker-compose up -d --build` (rebuild), `docker-compose logs -f` (tail logs).
- Migrations: `cd game.api && dotnet ef migrations add <Name> && dotnet ef database update`.
- E2E tests: `cd game.client && npm test` (Playwright; report via `npm run test:report`).

## Coding Style & Naming
- C# (.NET 8): 4 spaces; `PascalCase` for public types/methods, `camelCase` for locals/fields; nullable enabled; use minimal APIs and dependency injection patterns already present.
- Vue/JS: Prettier config in `game.client/.prettierrc.json` (4 spaces, single quotes, semicolons). ESLint in `.eslintrc.cjs` with Vue 3 rules. Components use `PascalCase.vue` (e.g., `DailyLeaderboard.vue`).
- Vue SFC order enforced: `script`, `template`, `style`.

## Testing Guidelines
- Frontend e2e: Playwright specs in `game.client/tests/*.spec.js`. Keep tests independent, default baseURL is `http://localhost:5000` (API behind Docker). Use `npm run test:ui` for local debugging.
- Backend: No unit tests yet; verify endpoints via Swagger or `curl` while Docker stack is running.

## Commit & PR Guidelines
- Prefer Conventional Commits: `feat(api): add leaderboard endpoint`, `fix(client): correct score parsing`, `refactor(ui): simplify table`.
- PRs include: clear description, linked issues, steps to verify (commands or screenshots), and note any schema/migration changes.
- Keep changes focused and small; update README if behavior or commands change.

## Security & Configuration
- Do not commit secrets. Use `.env` (see `.env.example`).
- Postgres defaults are dev-only. Override `ConnectionStrings__DefaultConnection` in deployments.

## Agent Notes
- Respect existing structure and formatting; avoid unrelated edits. If adding files, follow directory conventions above and lint before submitting (`npm run lint`).
