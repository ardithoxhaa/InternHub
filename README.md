# InternHub

InternHub is a full-stack intern onboarding workspace for managing interns, departments, onboarding tasks, company assets, documents, invites, notifications, audit history, reporting, and team chat from one dashboard.

The project is built as a portfolio-ready business application: Angular on the frontend, ASP.NET Core on the backend, SQL Server with EF Core migrations, JWT authentication, role-aware workflows, SignalR team chat, and optional AI/email integrations.

## Features

- JWT login/register flow with role-aware navigation
- Intern profiles with onboarding progress, documents, assets, notifications, and timeline history
- Department, task, asset, invite, member, settings, and audit management
- Launch wizard for creating an intern profile, onboarding plan, invite, and starter asset
- Reusable onboarding templates
- Calendar, analytics, CSV exports, reminders, and search
- SignalR team chat
- Optional AI assistant integration
- Responsive dashboard UI with desktop/tablet/mobile layouts

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 21, TypeScript, SCSS |
| Backend | ASP.NET Core Web API, .NET 9 |
| Database | SQL Server LocalDB, Entity Framework Core |
| Realtime | SignalR |
| Auth | JWT bearer authentication |
| Tooling | Angular CLI, npm, Swagger/OpenAPI |

## Screenshots

Add screenshots before publishing the repository publicly:

```text
docs/screenshots/landing.png
docs/screenshots/dashboard.png
docs/screenshots/intern-profile.png
docs/screenshots/launch-wizard.png
```

Recommended GitHub README image syntax:

```md
![InternHub dashboard](docs/screenshots/dashboard.png)
```

## Project Structure

```text
InternHub.sln
InternHub.Api/
  Contracts/          Request/response DTOs
  Controllers/        API endpoints
  Data/               DbContext and seed data
  Hubs/               SignalR hubs
  Middleware/         Error handling middleware
  Migrations/         EF Core migrations
  Models/             Database entities
  Services/           Auth, email, audit, AI helpers
internhub-client/
  src/app/core/       API configuration
  src/app/dashboard/  Dashboard UI components
  src/app/landing/    Public landing page
  src/app/layout/     App shell/sidebar
  src/app/shared/     Reusable UI pieces
```

## Prerequisites

- .NET 9 SDK
- Node.js and npm
- SQL Server LocalDB

Check your tools:

```powershell
dotnet --version
node --version
npm --version
```

## Backend Setup

From the repository root:

```powershell
cd InternHub.Api
dotnet restore
dotnet run
```

Development URLs:

```text
http://localhost:5170
https://localhost:7143
```

Swagger:

```text
http://localhost:5170/swagger
```

The API applies EF Core migrations and seeds demo data on startup.

## Frontend Setup

Open a second terminal:

```powershell
cd internhub-client
npm install
npm run start
```

Open:

```text
http://localhost:4200
```

## Demo Login

```text
Email: admin@internhub.test
Password: Admin123!
```

## Configuration

Development settings live in:

```text
InternHub.Api/appsettings.Development.json
```

Default SQL Server LocalDB connection:

```text
Server=(localdb)\MSSQLLocalDB;Database=InternHubDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Optional integrations:

- Email setup: `EMAIL_SETUP.md`
- AI assistant setup: `AI_SETUP.md`

## API Areas

The backend includes endpoints for:

- Auth
- Dashboard
- Employees / intern profiles
- Departments
- Onboarding tasks
- Company assets
- Documents
- Notifications
- Onboarding templates
- Calendar
- Reports / CSV exports
- Product operations such as search, invites, settings, analytics, reminders, and chat history

Use Swagger in development for the full endpoint list.

## Build

Backend:

```powershell
cd InternHub.Api
dotnet build
```

Frontend:

```powershell
cd internhub-client
npm run build
```

## Future Improvements

- Split the Angular root component into feature-level pages and services
- Add route guards and real Angular routes for each workspace area
- Add pagination/server-side filtering for large tables
- Replace browser `confirm()` with a styled confirmation dialog
- Add request/response validation with FluentValidation or a similar validation layer
- Add automated backend tests and frontend component tests
- Add production deployment documentation

## Notes For GitHub

Generated folders and local files are ignored by `.gitignore`, including:

- `bin/`
- `obj/`
- `node_modules/`
- `.angular/`
- `dist/`
- logs and local environment files

## Author

Built by Ardit Hoxha as a full-stack intern onboarding portfolio project.
