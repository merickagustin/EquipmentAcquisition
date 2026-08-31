# Equipment Acquisition Recording System

A sample equipment-acquisition tracking system for a large organization —
requests, approvals, purchase orders, and asset assignment across
departments. Built to demonstrate a real, measured SQL Server
optimization and a layered .NET Web API side by side. Full design detail
lives in [`docs/`](docs/), starting with
[`docs/architecture.md`](docs/architecture.md) and
[`docs/table-design.md`](docs/table-design.md).

## Scope

This is a demo to showcase a measured SQL Server optimization and a
sample architecture for running a background service, not a
production-ready product. The React bundle is a sample of how the
approach could slot into an existing enterprise web architecture
incrementally — a page at a time — rather than a proposal to replace
its frontend entirely.

## Tech stack

| Layer | Stack |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API, EF Core 8 (SQL Server provider), Swashbuckle/Swagger |
| Database | SQL Server 2022, runs in Docker — no local install needed |
| Frontend | React 18, TypeScript, Material UI 5 (pinned — see `architecture.md`), Webpack 5 |
| Seeding | Bogus (fake data), `SqlBulkCopy` for the three high-volume tables |
| Testing | xUnit + Moq (backend, 33 tests), Playwright (real headless-browser E2E) |
| DevOps | Docker Compose (3 services), Git/GitHub |

Ten independent React bundles (`nav`, `home`, `menu-admin`,
`vendors-admin`, `departments-admin`, `equipment-categories-admin`,
`requests-admin`, `assets-admin`, `reports-admin`, `purchase-orders-admin`)
— one per sidebar page, not a single SPA. See `architecture.md` for why.

## Docker (recommended)

One command brings up the whole stack — SQL Server, the API, and the Web
app, all wired together:

```bash
docker compose up --build
```

**First run pulls/builds several images — kick this off before doing
anything else,** ideally before reading the rest of this file, since it
runs in the background while you read. The SQL Server image alone is the
long pole: ~625MB compressed, and on a slow or congested connection the
pull can take **10–20+ minutes** — measured directly on this machine, not
a guess. A stalled-looking terminal during this step is normal, not a
hang; `docker images` / `docker ps` from another terminal will show
partial layers still downloading. Everything (SQL Server, the .NET
SDK/runtime, and Node base images) is cached after the first run, so this
cost is paid exactly once.

| Service | Reachable at | Notes |
|---|---|---|
| `web` | **http://localhost:8090** | Open this one |
| `api` | http://localhost:8081/swagger | The API directly, if you want to poke at endpoints |
| `sqlserver` | `localhost,1433` | Tools only — see "Accessing the database" below |

Migrations apply automatically on startup — no manual `dotnet ef
database update` step needed here. Seeding is still a one-time manual
step (25,000 `AcquisitionRequest` rows + `MenuItem` seed, ~7 seconds):

```bash
docker compose run api dotnet EquipmentAcquisition.Api.dll --seed
```

**http://localhost:8090/** (Home) shows a Pending Requisitions by
Department widget — every department, including zero-pending ones, sorted
by count. It only appears while the Acquisitions → Requests menu entry is
active; toggle it off in Menu Admin and the widget is replaced by a note
saying so, toggle it back on and it reappears. Not just cosmetic — it's
reading the exact same `IsActive` flag that controls whether `/requests`
is reachable from the sidebar at all.

Then visit **http://localhost:8090/menu-admin** for the full CRUD demo —
sidebar tree on the left, indented table on the right. Toggling `IsActive`
on a row updates the sidebar live. The same list/dialog CRUD pattern (built
on the shared `FormDialog` template) also powers three reference-data
admin pages, all reachable from the sidebar under `Administration`:
**Vendors** (`/vendors`), **Departments** (`/departments`), and
**Equipment Categories** (`/equipment-categories`).

**Acquisition Requests** (`/requests`, under the sidebar's `Acquisitions`
group) is the more involved demo — a paginated, filtered grid (Department +
Status + date range are mandatory, matching the cache table's composite
index; Equipment Category is an optional extra filter) reading from
`EquipmentAcquisitionDetailCache` rather than the base tables. Create a
request, then Approve or Reject it (Pending rows only); once Approved, its
row grows a shopping-cart action to create/edit/remove the linked Purchase
Order. Deleting a request is a **soft** delete (`IsDeleted`, on both the
request and its cache row) — it disappears from every view exactly like a
real delete, but the row and its audit history survive.

**Purchase Orders** (`/purchase-orders`, under `Acquisitions`) is a second,
standalone entry point onto the same data — a paginated list with optional
Vendor/Acquisition-Request-id filters. Creating one here uses a dropdown of
Approved requests with no PO yet, each option showing full detail (item,
department, requester, quantity, cost) so it's unambiguous which real
request you're attaching a PO to. **PO Number is generated, not typed** —
`PO-{year}-{id}`, assigned right after the row is created, the same format
the seed data uses. Deleting a PO here is also a **soft** delete, same
reasoning as Requests: the row (and any audit history) survives, and the
request it was for becomes eligible for a replacement PO again.

**Asset Registry** (`/assets`, under `Assets`) is a paginated CRUD list —
optional Department/Status filters, and creating one asks for a Purchase
Order id with an inline lookup (vendor/PO number/total shown before you
commit) rather than a picker, since Assets has no equivalent read-optimized
cache and browsing ~45k rows unfiltered isn't the point.

**Department Spend Report** (`/reports/department-spend`, under `Reports`)
is the read-only frontend for the actual centerpiece deliverable — the
measured naive-vs-optimized stored procedure (see `table-design.md`).
Filter by date range and an optional department; the table sorts by spend
descending with a grand-total footer row.

The background-service pattern behind that grid (`CacheRefreshQueue` +
`DetailCacheRefreshWorker` polling it on a timer) isn't inherently tied to
display — it's a general staging queue for "something needs to happen
asynchronously, after this write." Today the only consumer refreshes a
read cache, but the same shape (enqueue on write, drain on a timer) is a
natural fit for staging outbound notifications too — e.g. alerting a
department head when a request needs approval, or flagging one that
crosses a budget threshold — for both the Acquisition Request display and
future reports, not just the cache table.

**Resetting to a clean slate:**

```bash
docker compose down -v
docker compose up --build
docker compose run api dotnet EquipmentAcquisition.Api.dll --seed
```

**Running the automated tests** still needs the .NET SDK locally even
when the app itself runs in Docker — the `Tests` project isn't
containerized:

```bash
dotnet test
```

Should report `Passed! - Failed: 0, Passed: 33`.

## Local development (without Docker)

Useful when actively working on the code — faster edit/run loop than
rebuilding images each time. SQL Server still runs in Docker either way
(see `docs/database-setup.md` for why); only the API and Web run natively.

```bash
# 1. Start SQL Server only
docker compose up -d sqlserver

# 2. Apply the schema
dotnet tool install --global dotnet-ef --version 8.0.*   # if not already installed
dotnet ef database update --project src/EquipmentAcquisition.Core --startup-project src/EquipmentAcquisition.Api

# 3. Seed the database (~7 seconds)
dotnet run --project src/EquipmentAcquisition.Api -- --seed

# 4. Run the API
dotnet run --project src/EquipmentAcquisition.Api
```

The API listens on **http://localhost:5068** — note this is a
*different* port than the Docker path above (5068 here vs. 8081 there);
don't mix URLs between the two.

**Running the Web project** (separate process, separate terminal):

```bash
cd client && npm install && npm run build && cd ..
dotnet run --project src/EquipmentAcquisition.Web
```

Listens on **http://localhost:5248**. Same `/menu-admin` demo as above,
just on this port instead.

Verified with a real headless-browser test (`client/verify.js`, uses
Playwright) — not just reviewed: navigates to the page, confirms the
table and sidebar render the correct seed data, then exercises a full
Create and Delete through the actual UI. Run it yourself with both
processes up: `cd client && npx playwright install chromium && node verify.js`.

**Interactive API testing:** with the API running, open
**http://localhost:5068/swagger** (or `:8081/swagger` if using Docker
instead). Every endpoint is listed — expand one, "Try it out", fill in
values, "Execute" — runs a real request against the real database.

**Resetting to a clean slate** — the seeder isn't *re-runnable* against
already-seeded data (it inserts explicit ids that would collide), but
it's safe to try: it detects existing data up front and stops cleanly
with a message rather than failing partway through with a raw SQL error.
To actually add fresh data again:

```bash
docker compose down -v
docker compose up -d sqlserver
# then repeat steps 2 and 3 above
```

## Accessing the database

Same connection details whether you're running the Docker path or local
dev — `sqlserver`'s host-mapped port (`1433`) doesn't change between them.
Reachable at `localhost,1433` on any machine that runs `docker compose up`
from this repo, since `localhost` always means "this machine," not a
fixed address. The one thing that can conflict: if a machine already has
a native SQL Server instance using port 1433, that port is taken and the
container can't bind it the same way. If that happens, change only the
host side of the port mapping in `docker-compose.yml` (e.g.
`"14330:1433"`) and use that port number in your connection string
instead — see the comment there.

| | |
|---|---|
| Server | `localhost,1433` |
| Auth | SQL Login |
| User | `sa` |
| Password | `YourStrong!Passw0rd` |
| Database | `EquipmentAcquisitionDb` |
| Extra | Enable/check "Trust server certificate" — it's a local dev cert |

Any of these work:

- **Azure Data Studio** (free, cross-platform) or **SQL Server Management
  Studio** — New Connection → paste the details above.
- **VS Code's "SQL Server (mssql)" extension** — Command Palette → "MS
  SQL: Connect."
- **`sqlcmd` inside the container**, no client install needed:
  ```bash
  docker exec -it equipmentacquisition-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -C -d EquipmentAcquisitionDb
  ```
  Then plain T-SQL, e.g. `SELECT TOP 10 * FROM AcquisitionRequests;` then `GO`.

Container needs to be running first — `docker ps` to check.
