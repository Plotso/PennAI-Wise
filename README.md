# PennAI Wise

> A personal expense tracking web application — built with AI-assisted "vibe coding" using GitHub Copilot (Claude Opus 4.6 for planning & Sonnet 4.6 for execution).

Users can register an account, log daily expenses with categories, filter and search their spending history, and explore monthly summaries through an interactive dashboard.

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Vue 3 (Composition API), Vite 6, Pinia, Vue Router 4, Axios, Chart.js + vue-chartjs, Tailwind CSS v4 |
| **Backend** | ASP.NET Core 10 Minimal API, Entity Framework Core 10, SQLite |
| **Auth** | JWT Bearer tokens, BCrypt password hashing |
| **Tooling** | .NET 10 SDK, Node.js ≥ 20, npm |

---

## Project Structure

```
PennAI-Wise/
├── backend/
│   └── PennaiWise.Api/          # ASP.NET Core 10 Minimal API
│       ├── Data/                # EF Core DbContext
│       ├── Program.cs
│       └── appsettings.json
└── frontend/                    # Vue 3 + Vite SPA
    ├── src/
    │   ├── router/index.js      # Vue Router routes
    │   ├── stores/auth.js       # Pinia auth store
    │   ├── services/api.js      # Axios instance
    │   └── views/               # Page components
    └── vite.config.js
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js ≥ 20](https://nodejs.org/) with npm

---

## Running Locally

### Backend

```bash
cd backend/PennaiWise.Api
dotnet run --launch-profile http
# API available at http://localhost:5014
# OpenAPI spec at http://localhost:5014/openapi/v1.json
```

> **First run:** Create the database by applying EF Core migrations once they exist:
> ```bash
> dotnet ef database update
> ```

### Frontend

```bash
cd frontend
npm install       # first time only
npm run dev
# Dev server at http://localhost:5173
```

The frontend proxies API calls to `http://localhost:5050/api`. Update `src/services/api.js` `baseURL` if you change the backend port.

### Running Both Together

Open two terminal tabs:

```bash
# Tab 1 — backend
cd backend/PennaiWise.Api && dotnet run --launch-profile http

# Tab 2 — frontend
cd frontend && npm run dev
```

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

## License

MIT
