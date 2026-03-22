# HMLR Lease Info

A .NET 8 backend service that consumes raw HMLR Schedule of Notice of Lease data from a mock API, parses it into structured lease information via an Azure Function, and serves it through a REST API. Includes a Vue 3 frontend for searching and viewing parsed lease data.

## Project Structure

| Project | Description |
|---------|-------------|
| `HmlrLeaseInfo.Core` | Shared library — domain models, interfaces, parsing engine |
| `HmlrLeaseInfo.Infrastructure` | Azure Table Storage repositories, HMLR HTTP client |
| `HmlrLeaseInfo.Api` | .NET 8 Minimal API — `GET /{titleNumber}` with HybridCache |
| `HmlrLeaseInfo.Functions` | Azure Function (isolated worker) — queue-triggered sync |
| `HmlrLeaseInfo.Web` | Vue 3 + Vite + TypeScript frontend |
| `HmlrLeaseInfo.*.Tests` | xUnit test projects for Core, Infrastructure, API, and Functions |

## Prerequisites

### macOS

```bash
# .NET 8 SDK
brew install dotnet@8

# Node.js (for Vue frontend)
brew install node

# Azurite (Azure Storage emulator)
npm install -g azurite

# Azure Functions Core Tools v4
brew tap azure/functions
brew install azure-functions-core-tools@4
```

### Windows

```powershell
# .NET 8 SDK — download installer from https://dotnet.microsoft.com/download/dotnet/8.0
# Or via winget:
winget install Microsoft.DotNet.SDK.8

# Node.js — download installer from https://nodejs.org/
# Or via winget:
winget install OpenJS.NodeJS

# Azurite (Azure Storage emulator)
npm install -g azurite

# Azure Functions Core Tools v4
npm install -g azure-functions-core-tools@4 --unsafe-perm true
# Or via winget:
winget install Microsoft.Azure.FunctionsCoreTools
```

### Verify installation

```bash
dotnet --version          # 8.x.x
node --version            # v18+ or v20+
azurite --version         # 3.x.x
func --version            # 4.x.x
```

## Setup

### 1. Restore packages

```bash
# .NET packages
dotnet restore HmlrLeaseInfo.sln

# Vue frontend
cd src/HmlrLeaseInfo.Web && npm install && cd ../..
```

### 2. Build

```bash
dotnet build HmlrLeaseInfo.sln
```

### 3. Configure the mock HMLR API connection

The Azure Function needs to know where the mock API is running. Edit `src/HmlrLeaseInfo.Functions/local.settings.json`:

```json
{
  "Values": {
    "HmlrApi__BaseUrl": "http://localhost:5203",
    "HmlrApi__Username": "username",
    "HmlrApi__Password": "password"
  }
}
```

Adjust `BaseUrl` if the mock API runs on a different port. Check the mock API's `Properties/launchSettings.json` for the correct port.

## Running

Start each service in a separate terminal, in this order:

```bash
# 1. Azurite (Azure Storage emulator)
#    --skipApiVersionCheck avoids version mismatches between the Azure SDK and Azurite
azurite --silent --skipApiVersionCheck

# 2. HMLR Mock API (provided project)
cd <path-to-mock-api>/HmlrApi && dotnet run

# 3. Azure Function (queue-triggered sync worker)
cd src/HmlrLeaseInfo.Functions && func start

# 4. REST API
cd src/HmlrLeaseInfo.Api && dotnet run

# 5. Vue frontend (development server — no build needed)
cd src/HmlrLeaseInfo.Web && npm run dev
```

The API listens on `http://localhost:5010` by default. The Vue dev server runs on `http://localhost:5173` and proxies `/api` requests to the API.

The API requires Basic Auth. Default development credentials are `username:password` (configured in `appsettings.Development.json`). The Vue frontend sends these automatically.

### Request flow

1. `GET /EGL557357` → **202 Accepted** — no data yet, triggers async sync via queue
2. The Azure Function picks up the queue message, fetches data from the mock API, parses it, and stores it in Table Storage
3. `GET /EGL557357` → **200 OK** — returns parsed lease data
4. `GET /NONEXISTENT` → **404 Not Found** — data is fresh, title not present

## Running Tests

### Unit tests

```bash
# All tests
dotnet test HmlrLeaseInfo.sln

# Individual test projects
dotnet test tests/HmlrLeaseInfo.Core.Tests/
dotnet test tests/HmlrLeaseInfo.Api.Tests/
dotnet test tests/HmlrLeaseInfo.Functions.Tests/

# Infrastructure tests (requires Azurite running)
azurite --silent --skipApiVersionCheck &
dotnet test tests/HmlrLeaseInfo.Infrastructure.Tests/
```

### End-to-end smoke test

**Setup:** Start all four backend services with a clean database (no prior Azurite data) as described in [Running](#running). Dev config uses `DataFreshness: 2min`, `RequestThrottle: 1min` for faster testing.

| # | Scenario | Command | Expected | Status |
|---|----------|---------|----------|--------|
| 1 | No auth header | `curl -s -o /dev/null -w "%{http_code}" http://localhost:5010/EGL557357` | `401` | PASS |
| 2 | Wrong credentials | `curl -s -o /dev/null -w "%{http_code}" -u wrong:creds http://localhost:5010/EGL557357` | `401` | PASS |
| 3 | First request (cold, never synced) | `curl -s -u username:password -w "\nHTTP %{http_code}\n" http://localhost:5010/EGL557357` | `202` — triggers async sync via queue | PASS |
| 4 | Retry after sync completes (~5s) | Same as #3 | `200` — returns parsed lease data | PASS |
| 5 | All five title numbers | Loop below | All `200` — sync fetches all entries in one pass | PASS |
| 6 | Non-existent title (fresh sync) | `curl -s -u username:password -w "\nHTTP %{http_code}\n" http://localhost:5010/NONEXISTENT` | `404` with `lastSyncAt` timestamp | PASS |
| 7 | Existing title after DataFreshness expires (2min) | Same as #3, wait 2+ minutes | `200` — stale-while-revalidate returns cached data, silently re-syncs | PASS |
| 8 | Immediate follow-up (within RequestThrottle) | Same as #3 | `200` — no duplicate queue message (throttled) | PASS |

**Test #5 — all titles:**

```bash
for t in EGL557357 TGL24029 TGL27196 TGL383606 TGL513556; do
  echo -n "$t: "; curl -s -u username:password -o /dev/null -w "%{http_code}" http://localhost:5010/$t; echo
done
# EGL557357: 200
# TGL24029: 200
# TGL27196: 200
# TGL383606: 200
# TGL513556: 200
```

**Key behaviours verified:**
- Basic Auth rejects unauthenticated/invalid requests (tests 1–2)
- Cold start triggers async sync, returns 202 until data is ready (tests 3–4)
- Single sync populates all entries from the HMLR API (test 5)
- Fresh sync returns 404 for absent titles instead of re-syncing (test 6)
- Stale data triggers background re-sync while still returning 200 immediately (test 7)
- Request throttle prevents duplicate queue messages within the throttle window (test 8)

## Configuration

Sync timing is controlled by `SyncOptions`, shared between the API and Function:

| Setting | Default | Purpose |
|---------|---------|---------|
| `DataFreshness` | `00:30:00` | How long parsed data is considered fresh. Controls cache TTL and re-sync triggers. |
| `RequestThrottle` | `00:05:00` | Minimum interval between queue messages. Prevents flooding the queue with duplicate sync requests. |

Values use `TimeSpan` format (`hh:mm:ss`). In the Function's `local.settings.json`, use double-underscore notation:

```json
"Sync__DataFreshness": "00:30:00",
"Sync__RequestThrottle": "00:05:00"
```

## Beyond the Requirements

Features added on top of the base task:

- **Async sync via Azure Queue + Function** — non-blocking `202` response with queue-triggered background parsing
- **HybridCache with stampede protection** — `GetOrCreateAsync` ensures only one factory runs per cache key, even under concurrent load
- **3-layer sync protection** — API request throttle (`HybridCache` deduplicates queue sends) → single-instance Function (`batchSize: 1`) → freshness gate (`CompletedAt` check). Prevents redundant syncs without complex distributed locking
- **Freshness-based re-sync** — stale data (> `DataFreshness`) returns 202 instead of 404, automatically triggering a re-sync
- **Self-healing on failure** — queue auto-retries failed syncs; if all retries exhaust, normal API usage queues a fresh message
- **Additive/update-only sync** — upserts by LesseesTitle, never deletes. Safe for legal documents that persist once published
- **Vue 3 frontend** — search by title number with status handling (200/202/404)
- **Configurable sync timing** — `SyncOptions` shared between API and Function with `TimeSpan` values
