# RISE – Rice Farmer Management System with Data Analytics

RISE is maintained as the existing ASP.NET Core / SQL Server / HTML-JavaScript application. This update narrows the active user interface to five modules:

1. Dashboard
2. Farmer Records
3. Assistance Distribution
4. Utilization Monitoring
5. Reports and Analytics

The intended workflow is **Register farmers → Record assistance distribution → Confirm receipt → Monitor utilization**.

## Technology

- ASP.NET Core Web API (`RISE.API`, .NET 11 target)
- Entity Framework Core and SQL Server
- Static HTML, CSS, JavaScript and Bootstrap 5 (`RISE.Web`)
- Chart.js for live dashboard charts

## Requirements

- .NET SDK matching the `net11.0` target framework
- SQL Server / SQL Server Express / LocalDB
- A modern browser
- VS Code Live Server (or another static web server) for the frontend

## Database configuration

Edit `RISE.API/appsettings.json` and set `ConnectionStrings:DefaultConnection` to your SQL Server instance. Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=RISE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

For production, use environment variables or user secrets rather than committing credentials.

The API applies Entity Framework migrations at startup (`Database.Migrate()`). The new migration `20261119150000_UtilizationMonitoring` creates the `UtilizationMonitorings` table and its one-to-one relationship to a distribution item. It does not drop or reset existing farmer, distribution, or lookup data.

To apply migrations manually from the repository root:

```bash
dotnet ef database update --project RISE.API
```

If the EF CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

The existing SQL scripts remain in `Database/` and `RISE.Database/`. The EF migration is the authoritative change for the utilization monitoring table.

## Run the API

From the project root:

```bash
dotnet restore RISE.API/RISE.API.csproj
dotnet run --project RISE.API/RISE.API.csproj --launch-profile http
```

The existing launch profile listens at `http://localhost:5000`. The API applies migrations at startup. The OpenAPI document is available at `/openapi/v1.json` in Development.

## Run the frontend

Open `RISE.Web/` using VS Code Live Server, normally at `http://127.0.0.1:5500`, or use another static HTTP server. The frontend API base URL is configured near the top of `RISE.Web/pages/css/js/api.js`:

```js
const API_BASE_URL = "http://localhost:5000/api";
```

If the API is hosted elsewhere, update this value and ensure the API CORS policy allows the frontend origin.

## Functional notes

- A distribution is shown as **Pending** until it is marked **Received**. The existing database workflow values (`Draft` / `Approved` / `Distributed`) are retained for compatibility; both pending states are grouped as Pending in the UI and analytics.
- Utilization is recorded per distributed item. Each item can have one editable monitoring record. Used, still stocked, not yet used, and disposed quantities are distinct values; their sum cannot exceed the received quantity.
- Blank utilization quantities remain unreported. The dashboard and reports show the remaining unreported quantity instead of treating it as zero.
- Reports can be printed or saved as PDF through the browser print dialog.
- Farmer deletion uses the existing archive behavior so historical distribution relationships are preserved.
- Farm area is not part of the existing farmer entity; the legacy farm model is retained in the database but is no longer a separate active navigation module.

## Simplified navigation / legacy data

The active navigation contains only the five requested modules. Farm, association, supplier, supply-catalog, planting, crop-monitoring, harvest, weather, audit, user, profile and settings screens are no longer exposed through the primary navigation. Their existing database objects and historical migrations have been retained where removing them could damage existing data or break foreign-key relationships. They are not required for the new workflow.

## Validation status

- JavaScript syntax validation: passed with `node --check`.
- ZIP integrity: verified after packaging.
- .NET build and SQL Server integration tests: not run in this environment because the .NET SDK and SQL Server are not available here. Run `dotnet build RISE.API/RISE.API.csproj` and test against a copy of your database before production use.
