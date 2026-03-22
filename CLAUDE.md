# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 8 C# backend service for parsing HMLR (HM Land Registry) Schedule of Notice of Lease data. Fetches raw data from a mock HMLR API, parses fixed-width tabular entries into structured records, persists to Azure Table Storage, and serves via a REST API with caching and async sync.

## Solution Structure

```
src/
  HmlrLeaseInfo.Core/          # Domain models, interfaces, parsing logic (zero dependencies)
  HmlrLeaseInfo.Infrastructure/ # Azure Table Storage repositories, HMLR HTTP client
  HmlrLeaseInfo.Api/            # ASP.NET Core REST API (HybridCache, Basic Auth, queue integration)
  HmlrLeaseInfo.Functions/      # Azure Functions queue-triggered sync worker
  HmlrLeaseInfo.Web/            # Vue 3 frontend (Vite dev server)
tests/
  HmlrLeaseInfo.Core.Tests/
  HmlrLeaseInfo.Infrastructure.Tests/
  HmlrLeaseInfo.Api.Tests/
  HmlrLeaseInfo.Functions.Tests/
```

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "ClassName=LeaseParserTests"

# Run API
dotnet run --project src/HmlrLeaseInfo.Api

# Run Function (requires Azurite running)
cd src/HmlrLeaseInfo.Functions && func start

# Run Vue frontend
cd src/HmlrLeaseInfo.Web && npm run dev
```

## Architecture

- **API endpoint**: `GET /{titleNumber}` — returns 200 (found), 202 (syncing), or 404 (not found)
- **Parsing**: Fixed-width column rules (73-char lines) with sticky-column continuation and trailing NOTE extraction
- **Caching**: HybridCache (in-memory L1) with `DataFreshness` TTL. `GetOrCreateAsync` provides stampede protection
- **Async sync**: API enqueues to Azure Queue → Function processes with serial execution (`batchSize: 1`, `maxConcurrentCalls: 1`) → freshness gate prevents redundant syncs
- **Auth**: Basic Auth on the API, matching the mock HMLR API pattern

## Key Configuration

Sync timing in `appsettings.json` / `appsettings.Development.json` per project:
- `DataFreshness` (default 30min, dev 2min) — cache TTL and sync staleness threshold
- `RequestThrottle` (default 5min, dev 1min) — API-only, minimum interval between queue sends

## Conventions

- File-scoped namespaces, primary constructors for DI
- C# records for immutable models
- XML doc comments: interfaces describe contract (what), implementations describe flow (how) on complex methods only
- No `/// <inheritdoc />`
- TDD: tests first, then implementation
- Squash merge via PR, no direct commits to main
