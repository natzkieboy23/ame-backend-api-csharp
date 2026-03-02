# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# From backend-api-csharp/InventoryApi/
dotnet run                              # Web API on http://localhost:5141 (Swagger at root)
dotnet build                            # Build only

# QB Sync Console — always use this, NOT dotnet run -- --sync while the API is running
.\run-sync.ps1                          # Compiles to bin\sync\ to avoid file-lock conflict
dotnet run -p:SyncBuild=true -- --sync  # Manual equivalent

# Release / packaging
.\publish.ps1                           # Publish self-contained win-x86 exe to publish\
# Then open installer.iss in Inno Setup and press F9
```

> **Dual-exe architecture:** `SyncBuild=true` changes the output path to `bin\sync\` so the sync console exe never conflicts with the running web API exe. Never omit this when running both simultaneously.

## Architecture

### Dual-Mode Entrypoint (`Program.cs`)
The single project produces one exe that behaves differently depending on args:
- `--sync` → `SyncConsole.RunAsync()` (interactive TUI, no ASP.NET)
- *(no args)* → ASP.NET Web API + Swagger on `http://0.0.0.0:5141`
- Can also run as a Windows Service (`sc.exe start AME-InventoryAPI`), registered by the Inno Setup installer.

### API Layer
- Controllers follow the standard ASP.NET pattern: `InventoryDbContext` injected via constructor.
- All responses use `ApiResponse<T>` wrapper (`{ success, message, data, totalCount }`).
- DELETE is always a **soft delete**: sets `IsActive = "false"` (or `Status = "Deleted"` for Bill), never removes rows.
- `EditSequence`: Unix timestamp on create (`EditSequenceHelper.Generate()`), integer-increment on update (`EditSequenceHelper.Increment()`).

### Data Layer
- **ORM**: EF Core 8 + Pomelo for MySQL 5.7.32. All entity properties carry `[Column("ExactName")]` — Pomelo defaults to snake_case without it.
- **Primary keys**: `ListID` (UUID string, `varchar(36)`) on most entities; `TxnID` on `Bill`.
- **Booleans**: stored as `varchar(5)` with literal strings `"true"` / `"false"` — no conversion layer.
- **FK references**: always stored as `*_ListID` + `*_FullName` pairs (denormalized).
- **Decimal precision**: all decimal columns use `HasPrecision(16, 6)` configured in `InventoryDbContext.OnModelCreating`.
- **Raw SQL**: `BillPush`, `ItemSync`, `VendorSync`, `InventorySiteSync` use **Dapper** directly (not EF) for upsert performance.

### QuickBooks Integration (`QuickBooks/`)
- `QbSession`: wraps the COM object `QBXMLRP2.RequestProcessor` (registered by QuickBooks Desktop). Negotiates the highest supported QBXML version via `HostQueryRq` on `Open()`.
- **Pull syncs** (`ItemSync`, `VendorSync`, `InventorySiteSync`): send a QBXML query, parse the XML response, upsert into MySQL with `INSERT … ON DUPLICATE KEY UPDATE`.
- **Push sync** (`BillPush`): reads bills with `Status = 'ADD'`, builds a batch `BillAddRq` QBXML, sends to QB, and on success sets `Status = 'Synced'` and stores the QB-assigned `TxnID` in `ExternalGUID`.
- QB sync requires QuickBooks Desktop to be installed and running on the same machine. First run triggers a one-time authorization dialog in QB.

### Bill Entity (unique pattern)
Unlike other entities, `Bill` has child `TxnItemLineDetail` rows with a cascade-delete FK (`IDKEY → bill.TxnID`). The `BillController` exposes sub-endpoints at `/api/bill/{txnId}/lines`. When creating a line item, `EnrichLineCostAsync` auto-fills `Cost` and `Amount` from the linked item's `PurchaseCost`.

### Database
- MySQL 5.7.32, database `amedb`. Connection string in `appsettings.json`.
- Schema DDL is in `schema/*.schema.sql` — these are reference files, not migration scripts.
- No EF migrations are used; schema is managed manually.

### Deployment
- `publish.ps1` produces a self-contained single-file `win-x86` exe.
- `installer.iss` (Inno Setup 6) packages the exe and `appsettings.json`, registers the Windows Service (`AME-InventoryAPI`, auto-start), and creates a desktop shortcut for the sync console.
- On upgrade, `appsettings.json` is not overwritten (`Flags: onlyifdoesntexist`).
