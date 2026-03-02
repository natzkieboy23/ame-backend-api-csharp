# AME Inventory API

A C# .NET 8 REST API for inventory management with QuickBooks Desktop synchronization, backed by MySQL 5.7.

---

## Requirements

| Dependency | Version |
|---|---|
| .NET SDK | 8.0 |
| MySQL | 5.7.32 |
| QuickBooks Desktop | Any (for QB sync only) |
| Inno Setup | 6+ (for installer builds only) |

QuickBooks must be installed on the **same machine** as the sync console. The COM object `QBXMLRP2.RequestProcessor` is registered by QuickBooks Desktop itself.

---

## Getting Started

### 1. Database

Create the `amedb` database and run the schema files in order:

```sql
-- Run each file in schema/
schema/inventorysite.schema.sql
schema/vendor.schema.sql
schema/iteminventory.schema.sql
schema/bill.schema.sql
schema/txnitemline.schema.sql
```

### 2. Configuration

Edit `InventoryApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=amedb;User=YOUR_USER;Password=YOUR_PASS;CharSet=utf8mb4;"
  },
  "QuickBooks": {
    "AppName": "AME Inventory",
    "CompanyFile": "",
    "QbXmlVersion": "10.0"
  }
}
```

- **`CompanyFile`** — leave empty to use whatever `.qbw` file is currently open in QuickBooks; or set a full path like `C:\Users\You\Company.qbw`.
- **`QbXmlVersion`** — the session auto-negotiates the highest version supported by your QB installation; this is a fallback only.

### 3. Run

```bash
cd InventoryApi
dotnet run          # API starts on http://localhost:5141
                    # Swagger UI is at the root: http://localhost:5141
```

---

## Running the QB Sync Console

> **Important:** If the web API is already running, do **not** use `dotnet run -- --sync` — it triggers a build that conflicts with the running exe. Always use `run-sync.ps1` instead.

```powershell
cd InventoryApi
.\run-sync.ps1
```

The console presents an interactive menu:

```
[1]  Sync Vendors          (QB → MySQL vendor table)
[2]  Sync Items            (QB → MySQL iteminventory table)
[3]  Sync Inventory Sites  (QB → MySQL inventorysite table)
[4]  Send Bills            (MySQL → QB, batch BillAdd)
[5]  Test Connections
[0]  Exit
```

**First run:** QuickBooks will show an authorization dialog asking you to grant access. Select "Yes, always allow access even if QB is not running" and click Continue. This is a one-time step per company file.

---

## API Endpoints

All responses are wrapped in `{ "success": bool, "message": string, "data": ..., "totalCount": int }`.

DELETE is always a **soft delete** — no rows are ever removed from the database.

### Inventory Sites
| Method | Route | Query params |
|--------|-------|-------------|
| GET | `/api/inventorysite` | `?isActive=` |
| GET | `/api/inventorysite/{id}` | |
| POST | `/api/inventorysite` | |
| PUT | `/api/inventorysite/{id}` | |
| DELETE | `/api/inventorysite/{id}` | sets `IsActive = "false"` |

### Item Inventory
| Method | Route | Query params |
|--------|-------|-------------|
| GET | `/api/iteminventory` | `?isActive=` `?manufacturerPartNumber=` `?siteFullName=` |
| GET | `/api/iteminventory/{id}` | |
| POST | `/api/iteminventory` | |
| PUT | `/api/iteminventory/{id}` | |
| DELETE | `/api/iteminventory/{id}` | sets `IsActive = "false"` |

> When `?siteFullName=` is provided, the response includes `SiteQuantityOnHand` — the sum of `itemsite.QuantityOnHand` for all sub-locations matching that site.

### Vendors
| Method | Route | Query params |
|--------|-------|-------------|
| GET | `/api/vendor` | `?isActive=` |
| GET | `/api/vendor/{id}` | |
| POST | `/api/vendor` | |
| PUT | `/api/vendor/{id}` | |
| DELETE | `/api/vendor/{id}` | sets `IsActive = "false"` |

### Bills
| Method | Route | Query params |
|--------|-------|-------------|
| GET | `/api/bill` | `?vendorRef_ListID=` `?isPaid=` `?status=` |
| GET | `/api/bill/{id}` | includes line items |
| POST | `/api/bill` | accepts optional `lineItems` array |
| PUT | `/api/bill/{id}` | header fields only |
| DELETE | `/api/bill/{id}` | sets `Status = "Deleted"` |
| GET | `/api/bill/{txnId}/lines` | |
| POST | `/api/bill/{txnId}/lines` | |
| PUT | `/api/bill/{txnId}/lines/{lineId}` | |
| DELETE | `/api/bill/{txnId}/lines/{lineId}` | hard delete |

Bill `Status` lifecycle: `ADD` → (QB sync pushes it) → `Synced`. Bills with `Status = "ADD"` are the pending queue picked up by the sync console option [4].

---

## QuickBooks Sync

### Pull (QB → MySQL)

Options [1]–[3] in the sync console each:
1. Send a QBXML query request to QuickBooks Desktop via COM (`QBXMLRP2.RequestProcessor`)
2. Parse the XML response
3. Upsert every record into MySQL using `INSERT … ON DUPLICATE KEY UPDATE`, keyed on `ListID`

### Push — Bills (MySQL → QB)

Option [4]:
1. Queries MySQL for all bills with `Status = 'ADD'` that have at least one line item
2. Shows a preview table and asks for confirmation
3. Builds a single batch `BillAddRq` QBXML document (all bills in one request, `onError="continueOnError"`)
4. Sends it to QuickBooks and parses each `BillAddRs`
5. On success: sets `Status = 'Synced'` and stores the QB-assigned `TxnID` in `ExternalGUID`
6. On failure: leaves the bill as `Status = 'ADD'` so it can be retried

When creating a bill line item via the API, `Cost` and `Amount` are automatically populated from `iteminventory.PurchaseCost` for the referenced item.

---

## Database Schema

Schemas are in `schema/*.schema.sql` — reference files only, not migration scripts. Apply them manually when setting up a new database.

Key conventions:
- **Primary keys**: `ListID` (`varchar(36)`, UUID) on most tables; `TxnID` on `bill`
- **Booleans**: stored as `varchar(5)` with values `"true"` / `"false"`
- **FK references**: always stored as both `*_ListID` and `*_FullName` (denormalized)
- **Decimals**: all `decimal(16,6)`
- **Extensibility**: `CustomField1`–`CustomField15` (`varchar(50)`) on `iteminventory`, `vendor`, `bill`, and `txnitemlinedetail`

---

## Deployment

### Build a release installer

```powershell
# 1. From backend-api-csharp\
.\publish.ps1
# Produces a self-contained single-file win-x86 exe in publish\

# 2. Open installer.iss in Inno Setup and press F9 (Build > Compile)
# Output: installer-output\AME-Inventory-Setup-1.0.0.exe
```

The installer:
- Copies `InventoryApi.exe` and `appsettings.json` to `%ProgramFiles%\AME Inventory`
- Registers and starts the Windows Service `AME-InventoryAPI` (auto-start)
- Creates a desktop shortcut that launches the sync console (`--sync` flag)
- On upgrade, **does not overwrite** an existing `appsettings.json` (preserves the database password and QB config)

### Run as a Windows Service (manual)

```cmd
sc create AME-InventoryAPI binPath= "C:\path\to\InventoryApi.exe" start= auto
sc start AME-InventoryAPI
sc stop AME-InventoryAPI
sc delete AME-InventoryAPI
```

---

## Project Structure

```
backend-api-csharp/
  InventoryApi/
    Controllers/        # API controllers (one per entity)
    Data/               # InventoryDbContext (EF Core)
    DTOs/               # Create / Update / Response DTOs per entity
    Helpers/            # EditSequenceHelper
    Models/             # EF Core entities
    QuickBooks/         # QB sync — QbSession, sync classes, BillPush
    appsettings.json    # Connection string + QB config
    run-sync.ps1        # Launches sync console without conflicting with the API
  schema/               # Reference DDL for each table
  publish.ps1           # Self-contained release build script
  installer.iss         # Inno Setup installer definition
```
