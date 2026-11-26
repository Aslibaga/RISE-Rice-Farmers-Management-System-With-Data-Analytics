# RISE Deployment Guide

## 1. Server (one PC or server that holds the database)
1. Install SQL Server Express and restore/create the `RISE_DB` database.
2. The API applies EF migrations automatically on startup.
3. Start the API:
   ```
   dotnet run --project RISE.API/RISE.API.csproj --urls http://localhost:5000
   ```
4. To run on a network address, change the URL, e.g. `--urls http://0.0.0.0:5000`.
5. Override the JWT secret with an environment variable before starting:
   ```
   Jwt__Key=<long-random-secret>
   ```
6. Override the database connection with:
   ```
   RISE_DB_CONNECTION="Server=<SERVER>;Database=RISE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

## 2. Client PCs
1. Install the packaged RISE Desktop app.
2. In the app data folder (`%APPDATA%/rise`), edit `server-config.json`:
   ```json
   {
     "connectionString": "Server=<SERVER>;Database=RISE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
3. Open **Settings → System → Server Connection** and set the **API Server URL** to the server address (e.g. `http://192.168.1.10:5000`), then reload.

## 3. Backup
Back up `RISE_DB` regularly with SQL Server Management Studio or:
```
sqlcmd -S <SERVER> -Q "BACKUP DATABASE RISE_DB TO DISK='C:\Backups\RISE_DB.bak' WITH INIT"
```

## 4. Notes
- All farmer/distribution/utilization data stays in the shared database — accounts work on every PC.
- Season creation archives monitoring records so previous seasons remain viewable.
