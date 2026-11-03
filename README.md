# Compassion Cloud - Gift of the Givers Relief Management System

ASP.NET Core 8 MVC web app + Azure Functions + shared Helpers library, with automated tests and an Azure Pipelines CI/CD pipeline.

| Project | Purpose |
|---|---|
| `CompassionCloud` | ASP.NET Core MVC web app (donations, volunteers, employee dashboard, tax certificates) |
| `CompassionCloud.Functions` | Azure Functions (isolated worker): `GenerateTaxCertificate`, `LogReliefUpdate` (writes to Azure Blob Storage) |
| `CompassionCloud.Helpers` | Shared class library, packaged as the `CompassionCloud.Helpers` NuGet package |
| `CompassionCloud.Test` | xUnit unit / integration tests, Selenium UI tests, load tests, k6 scripts |

## Run locally
1. Start Azurite (or use the Azure Storage Emulator) for the Functions' blob storage.
2. Run the Functions project (`func start` or F5) - it listens on `http://localhost:7071`.
3. Run the web app (`dotnet run --project CompassionCloud`) - default URL `http://localhost:5031`.
   The database is created/migrated automatically on startup (LocalDB by default).

### Seeded demo accounts
| Role | Email | Password |
|---|---|---|
| Employee | employee@compassioncloud.com | Employee@123 |
| Admin | admin@compassioncloud.com | Admin@123 |
| Donor | donor@compassioncloud.com | Donor@123 |
| Volunteer | volunteer@compassioncloud.com | Volunteer@123 |

These are demo credentials - change or remove them before any real production use.
Public registration only offers **Donor** and **Volunteer**; Employee/Admin accounts are never self-assigned.

## Tests - see [TESTING.md](TESTING.md)
```
dotnet test --filter "Category!=UI&Category!=Load"
```

## CI/CD
`azure-pipelines.yml` builds and tests on every commit, then (master/main only) deploys the SQL schema, web app and Functions to Azure.
