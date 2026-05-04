# InternHub

InternHub is a full-stack intern onboarding workspace. It helps teams manage interns, departments, onboarding tasks, company assets, documents, invites, notifications, audit history, reports, and team chat from one clean dashboard.

## Features

- JWT authentication with role-aware navigation
- Intern profiles with onboarding progress and timeline views
- Department, task, asset, document, invite, and account management
- Onboarding templates and a launch wizard for creating intern workflows
- Calendar, analytics, CSV exports, notifications, and audit logs
- SignalR team chat
- Optional AI assistant integration
- Responsive Angular frontend with a cleaned-up dashboard design

## Tech Stack

- Frontend: Angular 21, TypeScript, SCSS
- Backend: ASP.NET Core Web API, .NET 9
- Database: SQL Server LocalDB with Entity Framework Core migrations
- Realtime: SignalR
- Auth: JWT bearer tokens

## Project Structure

```text
InternHub.sln
InternHub.Api/        ASP.NET Core backend
internhub-client/     Angular frontend
AI_SETUP.md           Optional AI setup notes
EMAIL_SETUP.md        Optional SMTP setup notes
```

## Prerequisites

- .NET 9 SDK
- Node.js and npm
- SQL Server LocalDB

Check versions:

```powershell
dotnet --version
node --version
npm --version
```

## Run The Backend

From the repository root:

```powershell
cd InternHub.Api
dotnet run
```

The API runs at:

```text
http://localhost:5170
https://localhost:7143
```

Swagger is available in development:

```text
http://localhost:5170/swagger
```

The backend runs EF Core migrations and seeds demo data on startup.

## Run The Frontend

Open a second terminal from the repository root:

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

The default development database connection is in:

```text
InternHub.Api/appsettings.Development.json
```

Default connection:

```text
Server=(localdb)\MSSQLLocalDB;Database=InternHubDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Optional setup:

- Email: see `EMAIL_SETUP.md`
- AI assistant: see `AI_SETUP.md`

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

## Notes For GitHub

Do not commit generated folders such as `bin`, `obj`, `node_modules`, `.angular`, or `dist`. The root `.gitignore` excludes those along with local logs and environment files.
