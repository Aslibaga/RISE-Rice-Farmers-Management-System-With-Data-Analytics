# RISE Web Frontend

The frontend is the existing static HTML/CSS/JavaScript application. It communicates with `RISE.API` and does not store farmer or assistance records locally.

## Run locally

1. Configure SQL Server in `RISE.API/appsettings.json`.
2. Start the API from the repository root:

   ```powershell
   dotnet run --project RISE.API/RISE.API.csproj --launch-profile http
   ```

   The existing HTTP profile listens on `http://localhost:5000`.

3. Start VS Code Live Server from `RISE.Web/` on port `5500`, then open `http://localhost:5500`.
4. If the API or frontend uses another origin, update `API_BASE_URL` in `pages/css/js/api.js` and the API CORS policy in `RISE.API/Program.cs`.

The API applies pending EF Core migrations at startup. To apply them manually:

```powershell
dotnet ef database update --project RISE.API
```

## Active modules

- Dashboard – live farmer, distribution and utilization totals and charts.
- Farmer Records – farmer CRUD, search, barangay filtering and individual assistance history.
- Assistance Distribution – records assistance items for registered farmers and marks records Pending or Received.
- Utilization Monitoring – one editable utilization record per received distribution item.
- Reports and Analytics – date, barangay, assistance type and text filters; browser printing supports Save as PDF.

## API routes used by the active workflow

- `/api/farmers` – farmer CRUD and search.
- `/api/distributions` – distribution CRUD, filtering and receipt confirmation.
- `/api/utilization` – list and upsert utilization monitoring.
- `/api/reports/dashboard` – live dashboard aggregates.
- `/api/reports/summary` – filtered distribution and utilization detail rows.

The existing SQL Server schema and older migrations are retained where needed to protect historical data and foreign-key compatibility. Their former screens are not exposed in the simplified frontend.

The distribution item selector reads item names and units from the existing active supply-item catalog. No supplier or warehouse screen is exposed. The current project has no implemented authentication/authorization middleware, so restrict API access to trusted development environments until an access-control layer is added.
