# PennAI Wise

PennAI Wise is a full-stack personal finance tracker that lets users register, record daily expenses across custom categories, and visualise spending patterns through an interactive monthly dashboard. It was built entirely through AI-assisted "vibe coding" using GitHub Copilot (Claude Opus 4.6 for planning, Sonnet 4.6 for execution) as an experiment in end-to-end AI-driven development. The project follows clean architecture conventions — a .NET 10 Minimal API backend with JWT auth, backed by SQLite, and a reactive Vue 3 SPA frontend with Chart.js visualisations.

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| **Frontend** | Vue (Composition API) | 3.5 |
| | Vite | 6.3 |
| | Pinia | 3.0 |
| | Vue Router | 4.6 |
| | Axios | 1.13 |
| | Chart.js + vue-chartjs | 4.5 / 5.3 |
| | Tailwind CSS | 4.2 |
| **Backend** | ASP.NET Core Minimal API | 10.0 |
| | Entity Framework Core | 10.0 |
| | SQLite | (via EF Core provider) |
| | BCrypt.Net-Next | 4.1 |
| **Auth** | JWT Bearer tokens + BCrypt password hashing | — |
| **Tooling** | .NET SDK | 10.0 |
| | Node.js | ≥ 20 |
| | npm | ≥ 10 |

---

## Project Structure

```
PennAI-Wise/
├── backend/
│   ├── PennaiWise.Api/          # ASP.NET Core 10 Minimal API
│   │   ├── Data/                # EF Core DbContext & seed data
│   │   ├── DTOs/                # Request / response shapes
│   │   ├── Endpoints/           # Minimal API route handlers
│   │   ├── Interfaces/          # Repository & unit-of-work contracts
│   │   ├── Migrations/          # EF Core migrations
│   │   ├── Models/              # Domain entities (User, Expense, Category)
│   │   ├── Repositories/Sqlite/ # EF Core SQLite implementations
│   │   ├── Services/            # TokenService (JWT)
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── PennaiWise.Tests/        # xUnit integration tests
└── frontend/                    # Vue 3 + Vite SPA
    ├── src/
    │   ├── components/          # Reusable UI components (charts, forms, nav)
    │   ├── router/index.js      # Vue Router routes
    │   ├── services/api.js      # Axios instance
    │   ├── stores/auth.js       # Pinia auth store
    │   └── views/               # Page-level components
    └── vite.config.js
```

---

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 |
| [Node.js](https://nodejs.org/) | ≥ 20 |
| npm | ≥ 10 (bundled with Node.js) |

---

## Running Locally

### 1. Backend

```bash
cd backend/PennaiWise.Api
dotnet run
# API:        http://localhost:5014
# OpenAPI:    http://localhost:5014/openapi/v1.json
```

> **First run only** — apply migrations to create the SQLite database:
> ```bash
> dotnet ef database update
> ```

### 2. Frontend

```bash
cd frontend
npm install        # install dependencies (first time only)
npm run dev
# Dev server: http://localhost:5173
```

### 3. Running Both Together

Open two terminal tabs:

```bash
# Tab 1 — backend
cd backend/PennaiWise.Api && dotnet run

# Tab 2 — frontend
cd frontend && npm install && npm run dev
```

---

## Running Tests

```bash
cd backend/PennaiWise.Tests
dotnet test
```

The test suite uses `WebApplicationFactory` to spin up an in-memory test host for integration-level coverage of the Auth, Expense, and Dashboard endpoints.

---

## Environment & Configuration

| File | Purpose |
|---|---|
| `backend/PennaiWise.Api/appsettings.json` | Production defaults (committed) |
| `backend/PennaiWise.Api/appsettings.Development.json` | Local overrides — **gitignored**, create manually |

Minimum `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pennaiwise-dev.db"
  },
  "Jwt": {
    "Secret": "<your-secret-at-least-32-chars>",
    "Issuer": "PennaiWise",
    "Audience": "PennaiWise"
  }
}
```

---

## Screenshots

> _Screenshots will be added manually once the UI is finalised._

| Screen | Preview |
|---|---|
| Login / Register | _(coming soon)_ |
| Dashboard (charts) | _(coming soon)_ |
| Expenses list | _(coming soon)_ |
| Add / edit expense | _(coming soon)_ |

---

## API Endpoints

All endpoints (except Auth) require a JWT Bearer token in the `Authorization` header.

### Auth — `/api/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register a new user; returns a JWT |
| `POST` | `/api/auth/login` | Public | Authenticate; returns a JWT |

### Expenses — `/api/expenses`

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/expenses` | Required | List paginated expenses; supports `startDate`, `endDate`, `categoryId`, `page`, `pageSize` query params |
| `GET` | `/api/expenses/{id}` | Required | Get a single expense by ID |
| `POST` | `/api/expenses` | Required | Create a new expense |
| `PUT` | `/api/expenses/{id}` | Required | Update an existing expense |
| `DELETE` | `/api/expenses/{id}` | Required | Delete an expense |

### Categories — `/api/categories`

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/categories` | Required | List all categories visible to the current user (system + personal) |
| `POST` | `/api/categories` | Required | Create a custom category |
| `DELETE` | `/api/categories/{id}` | Required | Delete a user-owned category (expenses are reassigned) |

### Dashboard — `/api/dashboard`

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/dashboard` | Required | Aggregated spending summary for a given `month` and `year` (defaults to current month) |

---

## Future Improvements

- [ ] **Recurring expenses** — flag an expense as monthly/weekly and auto-generate future entries
- [ ] **Budget limits per category** — set a monthly budget cap and receive an in-app warning when close to the limit
- [ ] **CSV / PDF export** — download the filtered expense list for offline use or tax records
- [ ] **Dark mode** — persist user theme preference via Pinia + `localStorage`
- [ ] **Multi-currency support** — store a currency code per expense and convert totals to a base currency on the dashboard
- [ ] **Mobile PWA** — add a service worker and web-app manifest for installable offline use
- [ ] **OAuth login** — allow sign-in with Google / GitHub in addition to email/password
- [ ] **End-to-end tests** — add Playwright tests covering the full register → add expense → view dashboard flow
- [ ] **Docker Compose** — containerise the API and serve the built frontend, making local setup a single `docker compose up`
- [ ] **CI pipeline** — GitHub Actions workflow running `dotnet test` and `npm run build` on every push

---

## License

MIT
