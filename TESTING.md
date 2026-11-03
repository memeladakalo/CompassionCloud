# Testing guide

All tests live in `CompassionCloud.Test`. Tests are tagged with `[Trait("Category", ...)]`.

| Category | What it covers | Needs a running site? | Runs in pipeline? |
|---|---|---|---|
| `Unit` | Helpers library, donation validation, both Azure Functions (incl. negative tests) | No | Yes |
| `Integration` | Donation form (controller) -> Azure Function (faked) -> database (EF in-memory) | No | Yes |
| `UI` | Selenium: anonymous donation + employee login | Yes | No (manual / post-deploy) |
| `Load` | 5 concurrent users, 40 requests each, p95 < 3 s | Yes | No (manual) |

## 1. Unit + integration tests (no setup)
```
dotnet test --filter "Category!=UI&Category!=Load" --logger "console;verbosity=normal"
```
Or Visual Studio: **Test > Test Explorer > Run All** (UI/Load tests will fail unless the site is running - run them separately).

## 2. Selenium UI tests
1. Start the web app (`dotnet run --project CompassionCloud`).
2. `dotnet test --filter "Category=UI"`
   - Another URL (e.g. the live site): set `COMPASSION_BASE_URL=https://<your-app>.azurewebsites.net`
   - No browser window: set `HEADLESS=true`
   - Requires Google Chrome installed (Selenium Manager downloads the matching driver).

## 3. Load test (5 concurrent users)
- C#: `dotnet test --filter "Category=Load" --logger "console;verbosity=detailed"` - prints average / median / p95 / failures.
- k6: `k6 run CompassionCloud.Test/CompassionCloud.Test/LoadTests/k6-load-test.js`
  (`-e BASE_URL=https://<your-app>.azurewebsites.net` for Azure)

## 4. Stress test (find the breaking point)
`k6 run CompassionCloud.Test/CompassionCloud.Test/LoadTests/k6-stress-test.js` - ramps 10 -> 25 -> 50 -> 100 users, then recovers.
Record: the stage where errors / latency rise, and whether the site recovers afterwards.

## 5. Testing the Functions manually (Postman / browser)
Start the Functions project, then `POST http://localhost:7071/api/taxcertificate/generate`
```json
{ "donorName": "Thandi", "donorEmail": "t@test.com", "amount": 250, "currency": "ZAR", "donationType": "One-time" }
```
and `POST http://localhost:7071/api/reliefupdates/log`
```json
{ "title": "Flood relief", "location": "Durban", "description": "Food parcels delivered", "postedBy": "employee@compassioncloud.com" }
```
Negative tests to run: invalid JSON, missing title/donorName, amount 0 or negative, currency `XYZ`. Each must return HTTP 400.

## 6. Health check
`GET /health` returns `{"status":"healthy"}` - handy right after a deployment.
