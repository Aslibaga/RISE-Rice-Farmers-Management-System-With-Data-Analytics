# RISE database scripts

Entity Framework Core migrations in `RISE.API/Migrations` manage the live schema. Apply migrations with:

```powershell
dotnet ef database update --project RISE.API
```

For an existing database, use EF migrations rather than running `Database/RISE_Database.sql`; the latter is a complete schema script for a new empty database only. Back up an existing database before applying changes.

The active RISE workflow uses farmer, distribution, distribution-item and utilization-monitoring data. The migration `20261119150000_UtilizationMonitoring` adds one utilization record per distribution item without resetting existing data. The matching SQL script is `004_UtilizationMonitoring.sql` in this folder and in `Database/`.

Other historical schema objects and scripts are retained for compatibility with existing databases and foreign-key history. They are not part of the simplified frontend workflow. The older distribution stored procedure is not used by the active API; receipt confirmation updates the distribution status without decrementing a separate warehouse stock balance.
