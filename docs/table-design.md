# Table Design — Equipment Acquisition Recording System

9 tables — 8 base tables plus one denormalized read model
(`EquipmentAcquisitionDetailCache`). Designed for a large org (many
departments) and a data volume (25,000 rows in `AcquisitionRequest`,
14,966 in `PurchaseOrder`, 44,956 in `Asset` — measured, see "Seeding
strategy," below) where the report query needs real optimization, not
just a LINQ one-liner. Lowered from an original 100k+ target specifically
to keep first-run wait reasonable for a reviewer — accepted as a real
trade against how dramatic the naive-vs-optimized report gap will look,
in exchange for a lighter demo.

Data access lives in one `EquipmentAcquisition.Core` project — DbContext,
repositories, services, DTOs and interfaces together, not split into
Application/Infrastructure (see `architecture.md` for why).

The frontend build (React + Material UI, multi-bundle — see
`architecture.md`) targets only one of these tables with full CRUD —
`MenuItem`, below. Everything else in the acquisition domain is API-only,
exercised via Swagger, not a React page.

**Scope reversal, recorded plainly:** this doc originally scoped the
acquisition domain's API surface to reads only, specifically to keep
effort weighted toward the report/C# work being assessed. That was
revisited — the domain now has full CRUD (`Department`, `Vendor`,
`EquipmentCategory`, `Employee`, `AcquisitionRequest` incl.
approve/reject actions, `PurchaseOrder`, `Asset`), because
`CacheRefreshQueue`/`AuditTrail`'s entire design assumes real writes to
trigger them — a read-only API would leave that orchestration built and
SQL-verified but never exercised live. Verified working end-to-end:
renaming a vendor via the API auto-refreshes the affected cache rows
within one `DetailCacheRefreshWorker` tick (~2s), with zero manual
intervention. See `Resolved`.

## Department
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| Code | varchar(10) | unique |
| Name | varchar(100) | |

## EquipmentCategory
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| Name | varchar(100) | unique |

## Vendor
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| Name | varchar(150) | |
| ContactEmail | varchar | nullable |

## Employee
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| DepartmentId | int FK | → Department, restrict delete |
| FullName | varchar(150) | |
| JobTitle | varchar(100) | nullable |

Minimal on purpose — no auth, no employment status. Wired only into
`AcquisitionRequest` (`RequestedByEmployeeId` / `ApprovedByEmployeeId`).
`Asset`'s custodian stays at the department level rather than gaining an
`Employee` FK of its own — extending person-level tracking there is a
separate decision this project doesn't need to make.

## AcquisitionRequest
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| DepartmentId | int FK | → Department, restrict delete |
| EquipmentCategoryId | int FK | → EquipmentCategory, restrict delete |
| RequestedByEmployeeId | int FK | → Employee, restrict delete |
| ItemDescription | varchar | |
| Justification | varchar | nullable |
| Quantity | int | |
| EstimatedCost | decimal(18,2) | |
| RequestDate | datetime | required |
| ApprovedDate | datetime | nullable |
| RejectedDate | datetime | nullable |
| ApprovedByEmployeeId | int FK, nullable | → Employee, set only once Status is Approved |
| RejectionReason | varchar | nullable |
| IsDeleted | bit, default 0 | soft delete — see below |

**Status is not a column.** It's derived: `RejectedDate` set → Rejected;
`ApprovedDate` set → Approved; else Pending. The two date columns are treated
as mutually exclusive by the application layer (a request that's rejected
doesn't later get an ApprovedDate).

**Index:** `(DepartmentId, EquipmentCategoryId, RequestDate)` — composite,
covers the report's grouping/filtering. The naive version of the report
deliberately runs before this index is added, so the before/after is real.

**Delete is soft, not physical.** `AcquisitionRequestService.DeleteAsync`
sets `IsDeleted = true` and saves — it no longer removes the row. A request
is business history the moment it exists (it may already carry an
Approve/Reject decision, and always carries an audit trail), so a delete
retiring it from view is the right default; physically erasing that history
isn't. `AcquisitionRequestRepository.GetAllAsync`/`GetByIdAsync` filter
`!IsDeleted`, so every normal caller — list, view, edit, approve, reject,
create-a-PO-against-it — sees a soft-deleted request exactly as "gone" as
the old hard-delete behavior. The `HasPurchaseOrderAsync` guard (can't
delete a request that already has a PO) is unchanged; it just now blocks a
flag flip instead of a row removal.

`EquipmentAcquisitionDetailCache` carries the same `IsDeleted` column,
mirrored by the refresh path — a soft-deleted request is still
re-materialized into the cache (not omitted), and
`DetailCacheRepository.GetPagedAsync` filters `!IsDeleted` unconditionally,
before the mandatory Department/Status/date triad. That split — refresh
mirrors the flag, the repository enforces it — keeps the refresh procs
simple (no "should I even insert this row?" branching) and keeps the
"what's visible" decision in exactly one place.

## PurchaseOrder
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| AcquisitionRequestId | int FK, unique (filtered) | 1:0-or-1 with AcquisitionRequest — see below |
| VendorId | int FK | → Vendor, restrict delete |
| PoNumber | varchar | unique, generated — see below |
| Quantity | int | |
| UnitCost | decimal(18,2) | |
| TotalCost | decimal(18,2) | stored, not computed on read (see note below) |
| OrderDate | datetime | |
| IsDeleted | bit, default 0 | soft delete — see below |

**Why TotalCost is stored, not computed:** at 25,000 rows, the report sums
cost across every matching row. Storing `TotalCost = Quantity * UnitCost` at
write time avoids a multiply-and-sum over the full row set on every report
run — a deliberate denormalize-for-read-performance call, kept in sync in
the application service layer rather than a DB trigger (simpler to reason
about, and this dataset has no concurrent-write concern).

**PoNumber is generated, not typed.** `PurchaseOrderService.CreateAsync`
inserts with a placeholder, lets SQL Server assign the identity `Id`, then
sets `PoNumber = "PO-{year}-{id:D6}"` and saves again — the same format the
seeder uses, so a runtime-created PO and a seeded one are indistinguishable.
Deriving the number from the row's own never-reused `Id` makes a collision
structurally impossible; there is no uniqueness check to write or bypass.

**Delete is soft, mirroring AcquisitionRequest, for the same reason** — a
PO is real business history (it may already have Assets tracked through
it) the moment it exists. `PurchaseOrderService.DeleteAsync` sets
`IsDeleted = true` instead of removing the row. Unlike AcquisitionRequest,
this is enforced via an EF Core global query filter
(`HasQueryFilter(x => !x.IsDeleted)`) rather than filtering each repository
method by hand — every LINQ query, including through
`AcquisitionRequest.PurchaseOrder`'s navigation property, sees only active
rows automatically. That matters here specifically: a manual per-method
filter would have missed the navigation-property path (`r.PurchaseOrder ==
null` silently doesn't apply an unrelated method's manual filter), which is
exactly the check the Create-PO dropdown depends on to know a request is
still eligible.

**The `AcquisitionRequestId` unique index is filtered**
(`WHERE IsDeleted = 0`), not plain unique — a soft-deleted PO must not
block a replacement. Without the filter, the old row would still count
toward uniqueness forever, and a request whose PO was "removed" could never
get a new one. `PoNumber`'s index stays unfiltered — it doesn't need the
same treatment, since generated numbers never collide regardless of delete
state.

**One place deliberately ignores the filter:**
`VendorRepository.HasPurchaseOrdersAsync`, the guard behind deleting a
Vendor. A soft-deleted PO still physically references its Vendor — the
FK doesn't go away — and the DB's Restrict constraint enforces that
regardless of `IsDeleted`. If this check respected the filter, it would
pass, then the actual `DELETE` would crash on the FK violation instead of
returning the clean `409` it returns today. Verified live: a Vendor with
only a soft-deleted PO against it still correctly refuses deletion.

## Asset
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| PurchaseOrderId | int FK | → PurchaseOrder, restrict delete |
| DepartmentId | int FK | current custodian dept — can differ from the request's |
| AssetTag | varchar(30), unique | e.g. "AST-000001" |
| SerialNumber | varchar | nullable |
| Status | int (enum) | InStock / Assigned / Maintenance / Retired |
| AcquiredDate | datetime | |
| LastUpdated | datetime | |

**Status here IS a real column** — unlike `AcquisitionRequest`, an asset's
lifecycle isn't linear (it can cycle Assigned → Maintenance → Assigned more
than once), so a set of nullable "state date" columns can't represent it
cleanly. This asymmetry is worth being able to explain out loud: dates work
for a one-way flow, an enum for a cyclical one.

**Index:** `(DepartmentId, Status)` — supports the Asset Registry grid's
department + status filter.

## EquipmentAcquisitionDetailCache

**Standalone — no relationships, by design.** Like `MenuItem`, this table
has no foreign keys to anything, in either direction. It is a flat,
self-contained row holding everything a request detail view needs, so
serving that view costs a single-key lookup and no joins, no aggregation,
and no `CASE` evaluation. The CPU that would go to re-deriving those values
on every read is spent once, at refresh time, instead.

One row per `AcquisitionRequest`, carrying values copied from
`Department`, `EquipmentCategory`, `Employee` (twice: requester and
approver), `PurchaseOrder` and `Vendor`.

| Column | Type | Notes |
|---|---|---|
| AcquisitionRequestId | int PK | The originating request's id as a **value**, not a reference — no FK. One row per request |
| DepartmentId | int | copy, not FK — see "standalone" above |
| DepartmentCode | varchar(10) | from Department |
| DepartmentName | varchar(100) | from Department |
| EquipmentCategoryId | int | copy |
| EquipmentCategoryName | varchar(100) | from EquipmentCategory |
| RequestedByEmployeeId | int | copy |
| RequestedByName | varchar(150) | from Employee |
| RequestedByJobTitle | varchar(100) | nullable, from Employee |
| ApprovedByEmployeeId | int | nullable, copy |
| ApprovedByName | varchar(150) | nullable, from Employee |
| ItemDescription | varchar | from AcquisitionRequest |
| Quantity | int | from AcquisitionRequest |
| EstimatedCost | decimal(18,2) | from AcquisitionRequest |
| RequestDate | datetime | from AcquisitionRequest |
| ApprovedDate | datetime | nullable |
| RejectedDate | datetime | nullable |
| Status | tinyint | **materialized** — see the tension note below |
| PurchaseOrderId | int | nullable — null until the request becomes a PO |
| PoNumber | varchar | nullable |
| VendorId | int | nullable, copy |
| VendorName | varchar(150) | nullable, from Vendor |
| UnitCost | decimal(18,2) | nullable |
| TotalCost | decimal(18,2) | nullable |
| OrderDate | datetime | nullable |
| RefreshedAt | datetime | when this row was last rebuilt — staleness is visible, not guessed |
| IsDeleted | bit | mirrors AcquisitionRequest.IsDeleted — see that section's "Delete is soft" note |

**Everything downstream of PurchaseOrder is nullable, and that's not
optional.** `PurchaseOrder` is 1:1 with `AcquisitionRequest` but *optional* —
a pending or rejected request never gets one. So `Vendor` is reachable only
through a PO that may not exist. Any query treating `VendorName` as present
is wrong for every non-approved request, which is most of the table early in
a fiscal year.

**No foreign keys at all.** Every `*Id` column is a value copy. Real FKs
would point `Restrict` delete paths from six directions into a table whose
defining property is that it can be truncated and rebuilt at will — and
would make writes to the base tables pay constraint-checking cost that the
whole point of this table is to avoid.

The trade that buys: **nothing at the database level keeps this table
consistent with its sources.** A deleted request leaves an orphan row; a
renamed department leaves a stale one. Neither is a bug in the schema —
both are the orchestration layer's responsibility, and that orchestration
is what defines whether this table is trustworthy. See Orchestration,
below.

**Indexes** — one composite for the grid's mandatory filter triad, plus
one single-column index per optional dimension:

| Index | Columns | Serves |
|---|---|---|
| `IX_..._DepartmentId_Status_RequestDate` | `DepartmentId, Status, RequestDate` | The grid's mandatory filters — Department, Status and a date range are never null. `RequestDate` sits last because it's a range predicate; anything after it in a composite loses seek benefit. |
| `IX_..._EquipmentCategoryId` | `EquipmentCategoryId` | Refresh proc resolution + optional grid filter, applied as a residual check over the already-narrowed triad seek |
| `IX_..._VendorId` | `VendorId` | Same, for vendor |
| `IX_..._RequestedByEmployeeId` | `RequestedByEmployeeId` | Same, for requester |
| `IX_..._ApprovedByEmployeeId` | `ApprovedByEmployeeId` | Same, for approver |

Deliberately **not** composite with the optional dimensions (e.g. no
`(DepartmentId, Status, VendorId, RequestDate)`) — since Department/Status/
Date are always present, the base triad already narrows the result before
any optional dimension is checked, so the residual filter is cheap. A
dedicated composite per optional dimension would mean 9 indexes total on
a table that's already rewritten on every refresh signal — only worth it
if a specific combination is measured as slow against real seeded data,
not built speculatively. Same reasoning is why there's no `VendorName`
index for name search: it's always layered on the mandatory triad too.

This is where the table actually earns its place: a departmental request
grid over 25,000 rows otherwise joins five tables before it can paginate.

### Grid pagination

`OFFSET`/`FETCH`, not keyset — page-number UI needs "jump to page N,"
which keyset pagination can't do (only next/previous from a cursor).
`OFFSET` does have a real cost on deep pages (SQL Server still walks and
discards every skipped row before reaching the requested page), but the
mandatory `Department` filter already caps how deep "deep" actually is —
a department's filtered slice, not the full 25,000-row table. Escalate to
keyset only if a specific department is measured as large enough for that
to matter once real seeded data exists.

```sql
SELECT ...
FROM dbo.EquipmentAcquisitionDetailCache
WHERE IsDeleted = 0   -- unconditional, not one of the optional filters — no toggle to see past it
  AND DepartmentId = @DepartmentId AND Status = @Status AND RequestDate BETWEEN @From AND @To
  -- optional filters (category/vendor/requester/approver) appended only when actually supplied —
  -- never as `(@Param IS NULL OR Col = @Param)`, which defeats every index above regardless of
  -- whether the param is actually null at runtime
ORDER BY RequestDate DESC, AcquisitionRequestId DESC   -- PK tiebreaker: RequestDate alone isn't
                                                        -- unique, so pagination can skip/duplicate
                                                        -- rows across pages without it
OFFSET (@PageNumber - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
```

The sort column is driven by whichever the grid is currently sorted by
(`RequestDate` is just the default) — the `AcquisitionRequestId` tiebreaker
always applies after it, whichever column that is. A paired
`COUNT(*)` query (same filters, no `ORDER BY`/`OFFSET`) drives the
"Page 3 of 47" total — it's index-only against the triad index, so it
stays cheap.

API contract this implies:
```
GET /api/acquisition-requests?departmentId=3&status=Pending&from=2026-01-01&to=2026-03-31
    &categoryId=2&pageNumber=3&pageSize=50&sortBy=RequestDate&sortDirection=desc
```

**Tension worth stating out loud:** `AcquisitionRequest.Status` is
deliberately *derived, not stored* (above), and this table stores it anyway.
That is a real contradiction, accepted knowingly — the base table stays the
single source of truth and the cache is a projection that may lag. The rule
is one-directional: nothing ever writes `Status` to the cache except the
refresh path, and nothing reads the cache to decide business logic. It backs
reads only.

### Orchestration

Every write to `AcquisitionRequest`, `PurchaseOrder`, `Department`,
`EquipmentCategory`, `Employee` or `Vendor` goes through the API — the
only thing with DB access (see `architecture.md`) — and, in the same
transaction as the business write, resolves the request(s) it affects and
enqueues them directly:

| Column | Type | Notes |
|---|---|---|
| Id | bigint identity PK | |
| AcquisitionRequestId | int | the request whose cache row needs rebuilding |
| EnqueuedAt | datetime2(3) | default `SYSUTCDATETIME()` |

**Resolution happens at write time, not at refresh time** — each write
path already knows, or can cheaply look up, which requests it affects, so
it resolves that directly rather than storing what changed and resolving
later:

```sql
-- AcquisitionRequest / PurchaseOrder writes — the affected id is already known
INSERT dbo.CacheRefreshQueue (AcquisitionRequestId) VALUES (@RequestId);

-- Vendor rename
INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
SELECT po.AcquisitionRequestId FROM dbo.PurchaseOrder po WHERE po.VendorId = @VendorId;

-- Department rename
INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
SELECT Id FROM dbo.AcquisitionRequest WHERE DepartmentId = @DepartmentId;

-- EquipmentCategory rename
INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
SELECT Id FROM dbo.AcquisitionRequest WHERE EquipmentCategoryId = @CategoryId;

-- Employee update (requester or approver)
INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
SELECT Id FROM dbo.AcquisitionRequest
WHERE RequestedByEmployeeId = @EmployeeId OR ApprovedByEmployeeId = @EmployeeId;
```

A `BackgroundService` inside `EquipmentAcquisition.Api` — not a separate
process, it starts and stops with the API — polls `CacheRefreshQueue` on a
`PeriodicTimer` (2s) and drains it in `@BatchSize = 2000`-row batches
(under SQL Server's ~5,000-lock escalation threshold), each batch its own
transaction (`SET XACT_ABORT ON`, so a failed insert can't leave a
committed delete behind). `READPAST` lets more than one API instance run
without two workers claiming the same row; `OUTPUT DISTINCT` on the drain
handles the same `AcquisitionRequestId` having been enqueued twice by
unrelated events landing close together.

Because resolution already happened at enqueue time, the refresh proc
itself needs no branching — every dequeued id gets the same full,
6-way-join rebuild:

```sql
CREATE OR ALTER PROCEDURE dbo.usp_RefreshAcquisitionDetailCache
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize int = 2000;
    -- OUTPUT doesn't support DISTINCT directly (a real bug in an earlier draft of this proc,
    -- caught only once actually run against SQL Server) — drain into a staging table
    -- (duplicates allowed), then dedup into the PK'd working table.
    CREATE TABLE #DrainedIds (AcquisitionRequestId int NOT NULL);
    CREATE TABLE #CurrentBatchAcquisitionRequestIds (AcquisitionRequestId int NOT NULL PRIMARY KEY);

    WHILE 1 = 1
    BEGIN
        ;WITH q AS (
            SELECT TOP (@BatchSize) Id, AcquisitionRequestId
            FROM dbo.CacheRefreshQueue WITH (READPAST)
            ORDER BY Id
        )
        DELETE FROM q
        OUTPUT deleted.AcquisitionRequestId INTO #DrainedIds (AcquisitionRequestId);
        IF @@ROWCOUNT = 0 BREAK;

        INSERT INTO #CurrentBatchAcquisitionRequestIds (AcquisitionRequestId)
        SELECT DISTINCT AcquisitionRequestId FROM #DrainedIds;

        BEGIN TRANSACTION;

            DELETE c
            FROM dbo.EquipmentAcquisitionDetailCache c
            INNER JOIN #CurrentBatchAcquisitionRequestIds b
                ON b.AcquisitionRequestId = c.AcquisitionRequestId;

            INSERT dbo.EquipmentAcquisitionDetailCache
                (AcquisitionRequestId, DepartmentId, DepartmentCode, DepartmentName,
                 EquipmentCategoryId, EquipmentCategoryName, RequestedByEmployeeId,
                 RequestedByName, RequestedByJobTitle, ApprovedByEmployeeId, ApprovedByName,
                 ItemDescription, Quantity, EstimatedCost, RequestDate, ApprovedDate,
                 RejectedDate, Status, PurchaseOrderId, PoNumber, VendorId, VendorName,
                 UnitCost, TotalCost, OrderDate, RefreshedAt)
            SELECT  r.Id, d.Id, d.Code, d.Name,
                    ec.Id, ec.Name,
                    re.Id, re.FullName, re.JobTitle,
                    ae.Id, ae.FullName,
                    r.ItemDescription, r.Quantity, r.EstimatedCost,
                    r.RequestDate, r.ApprovedDate, r.RejectedDate,
                    CASE WHEN r.RejectedDate IS NOT NULL THEN 2
                         WHEN r.ApprovedDate IS NOT NULL THEN 1
                         ELSE 0 END,
                    po.Id, po.PoNumber, v.Id, v.Name,
                    po.UnitCost, po.TotalCost, po.OrderDate, SYSUTCDATETIME()
            FROM        dbo.AcquisitionRequest  r
            INNER JOIN  #CurrentBatchAcquisitionRequestIds b ON b.AcquisitionRequestId = r.Id
            INNER JOIN  dbo.Department          d  ON d.Id  = r.DepartmentId
            INNER JOIN  dbo.EquipmentCategory   ec ON ec.Id = r.EquipmentCategoryId
            INNER JOIN  dbo.Employee            re ON re.Id = r.RequestedByEmployeeId
            LEFT  JOIN  dbo.Employee            ae ON ae.Id = r.ApprovedByEmployeeId
            LEFT  JOIN  dbo.PurchaseOrder       po ON po.AcquisitionRequestId = r.Id
            LEFT  JOIN  dbo.Vendor              v  ON v.Id  = po.VendorId
            OPTION (RECOMPILE);   -- plan sized to this batch, not shared with whichever ran first

        COMMIT;

        TRUNCATE TABLE #DrainedIds;
        TRUNCATE TABLE #CurrentBatchAcquisitionRequestIds;
    END
END
```

`usp_RebuildAllAcquisitionDetailCache` is unchanged by any of this —
`TRUNCATE` plus one `INSERT...SELECT` over every request, for the one case
where every row needs rebuilding at once: after the Bogus seeder writes
the base tables directly and leaves the cache empty.

**Considered and set aside, decided knowingly:** a column-scoped refresh
path was designed in detail — patch just `VendorName` on a vendor rename
instead of re-deriving the whole row, skipping the join back to
`AcquisitionRequest` entirely for label-only changes. Set aside in favor
of the simpler design above, where every signal becomes an id and every
id gets the same full rebuild — at the cost of moving fan-out resolution
onto the write path itself (a vendor rename touching 1,000 requests now
runs that `SELECT`+`INSERT` of 1,000 queue rows synchronously, before the
rename's own transaction can commit) and losing the join-avoidance
optimization the column-scoped version would have given. Worth
reconsidering if either cost is measured and found to matter once real
seeded data exists — the column-scoped design is fully worked out and
recoverable, not lost.

**Failure modes accepted, not engineered around:**
- **A signal can be lost** if the API process is killed between the
  `DELETE` that claims it and the transaction that completes its rebuild —
  dequeue removes a signal before its work is guaranteed done. The fix
  (claim-then-delete, with a reaper for abandoned claims) is a known
  escalation, not an unrecognized gap.
- **A write outside the API bypasses the queue entirely** — the Bogus
  seeder, or manual SQL run while investigating execution plans. This is
  why `usp_RebuildAllAcquisitionDetailCache` exists as a backstop that
  repairs drift from any cause, not only the tracked one.
- **The API is the only thing that can produce or drain a signal**, so
  stopping it stops both halves together — no backlog builds while it's
  down. The queue lives in SQL Server, not in memory, so whatever
  accumulated drains on the next tick after restart.
- **A large fan-out blocks its triggering write** for as long as the
  resolution query and multi-row insert take — accepted as the cost of
  keeping the refresh proc branch-free; see above.

**Not a substitute for the report optimization.** The department-spend report
still runs against `AcquisitionRequest`/`PurchaseOrder` with its composite
index. Pointing the report at this cache would replace the measured
naive-vs-optimized story with "I precomputed it," which is the one thing the
assessment is actually scoring. This table serves detail and list reads; the
report does not touch it.

## MenuItem
| Column | Type | Notes |
|---|---|---|
| Id | int PK | |
| ParentId | int FK, nullable, self-referencing | null = top-level item |
| Label | varchar(100) | |
| Route | varchar(200) | e.g. "/requests", "/assets", "/reports" |
| DisplayOrder | int | |
| IsActive | bit | |

Standalone from the acquisition domain — no FK to Department, Employee,
etc. This is the **full CRUD showcase**: a self-referencing table, served
flat by the API and rendered as a tree by the `nav` React bundle, with a
separate `menu-admin` React bundle for List/Create/Edit/Delete — replacing
the real legacy `Web.sitemap` + `asp:Menu` hardcoded-menu approach with an
actual data-driven menu. See `architecture.md` for the multi-bundle setup.

**Delete behavior is Restrict, not by choice but by necessity** — SQL
Server rejects a cascade path on a self-join (multiple-cascade-paths
error), so reparenting or blocking a delete with children is an
application-layer decision either way.

**Index:** `(ParentId, DisplayOrder)` — orders siblings under the same parent.

### Seed — five top-level menus

Hand-authored, not generated. Twelve rows, two levels deep, four parents
with ordered siblings — small enough to write by hand, structured enough
that the flat-table-to-nav-tree transform is actually exercised rather
than implied.

```
Home                                    /
Acquisitions            ── group ──
   ├─ Requests                          /requests
   └─ Purchase Orders                   /purchase-orders
Assets                  ── group ──
   └─ Asset Registry                    /assets
Reports                 ── group ──
   └─ Department Spend                  /reports/department-spend
Administration          ── group ──
   ├─ Menu Admin                        /menu-admin
   ├─ Vendors                           /vendors
   └─ Departments                       /departments
```

| Id | ParentId | Label | Route | DisplayOrder | IsActive |
|---|---|---|---|---|---|
| 1 | null | Home | `/` | 1 | true |
| 2 | null | Acquisitions | *null — group* | 2 | false |
| 3 | 2 | Requests | `/requests` | 1 | false |
| 4 | 2 | Purchase Orders | `/purchase-orders` | 2 | false |
| 5 | null | Assets | *null — group* | 3 | false |
| 6 | 5 | Asset Registry | `/assets` | 1 | false |
| 7 | null | Reports | *null — group* | 4 | false |
| 8 | 7 | Department Spend | `/reports/department-spend` | 1 | false |
| 9 | null | Administration | *null — group* | 5 | true |
| 10 | 9 | Menu Admin | `/menu-admin` | 1 | true |
| 11 | 9 | Vendors | `/vendors` | 2 | false |
| 12 | 9 | Departments | `/departments` | 3 | false |

**Why most rows are inactive.** `/menu-admin` is the only route with a
shell page in this scope (see `architecture.md`). Seeding the rest as
active would give a reviewer a menu where nine of twelve links 404 — the
first thing they would click. Inactive, `nav` renders `Home` and
`Administration → Menu Admin`, both of which work.

It also produces the demo that makes the whole exercise land: open Menu
Admin, toggle `IsActive` on **Reports**, reload, and watch the menu grow a
new branch. That is the difference between a hardcoded `Web.sitemap` and a
menu that is data — shown in ten seconds, using the CRUD being built.

**Two rules this seed depends on:**

- **`Route` must be nullable.** Rows 2, 5, 7 and 9 are group headers —
  "Acquisitions" expands, it does not navigate. The column is currently
  documented as non-nullable above, which would force `"#"` or `""` into
  those four rows. **This is a live inconsistency between the seed and the
  column definition, and it has to be resolved before either is built.**
  Recommendation: make `Route` nullable and treat null as "expand only".
- **A node renders only if it and every ancestor is active.** Otherwise
  deactivating "Acquisitions" leaves "Requests" floating at top level.
  This is a `nav` rule, not a database one — the API returns all rows so
  `menu-admin` can display and toggle inactive ones.

**Open:** does a shell page exist at `/`? Only the `menu-admin` page is
described anywhere. If there is no `Index.cshtml`, either add a trivial
one or point row 1 at `/menu-admin` and drop `Home`.

## AuditTrail

Cross-cutting — not part of the acquisition domain or the `MenuItem`
CRUD showcase, applies to writes on either. No auth exists in this
project (`Employee` is explicitly "no auth, no employment status," and no
API request currently carries an authenticated actor), so this records
*what* changed and *when*, not yet *who*.

| Column | Type | Notes |
|---|---|---|
| Id | bigint identity PK | |
| TableAffected | varchar(40) | e.g. `'Vendor'`, `'AcquisitionRequest'` — open-ended, not restricted to the cache-feeding tables |
| AffectedId | int | PK of the row that changed within `TableAffected` |
| Action | varchar(10) | `'Insert'` / `'Update'` / `'Delete'` |
| OldValues | nvarchar(max) | nullable — JSON snapshot before the change, null for Insert |
| NewValues | nvarchar(max) | nullable — JSON snapshot after the change, null for Delete |
| ChangedByEmployeeId | int | nullable, FK → Employee, restrict delete |
| DateApplied | datetime2(3) | default `SYSUTCDATETIME()` |

```sql
CREATE TABLE dbo.AuditTrail (
    Id                    bigint IDENTITY PRIMARY KEY,
    TableAffected         varchar(40)     NOT NULL,
    AffectedId            int             NOT NULL,
    Action                varchar(10)     NOT NULL,
    OldValues             nvarchar(max)   NULL,
    NewValues             nvarchar(max)   NULL,
    ChangedByEmployeeId   int             NULL,
    DateApplied           datetime2(3)    NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_AuditTrail_Action CHECK (Action IN ('Insert', 'Update', 'Delete')),
    CONSTRAINT FK_AuditTrail_Employee FOREIGN KEY (ChangedByEmployeeId) REFERENCES dbo.Employee(Id)
);
```

**Index:** `(TableAffected, AffectedId, DateApplied)` — "show me this row's
history, in order," the same one-purpose-built-index pattern the rest of
the schema follows.

**`OldValues`/`NewValues` are split, not one `Values` column** — a single
column only shows the current state, losing the ability to answer "what
did it change *from*," which is the actual point of an audit trail. Both
are JSON since different tables have different columns and this has to
work generically across all of them.

**`ChangedByEmployeeId` will be `NULL` on every row for as long as there's
no auth** — it's scaffolded for when an identity exists on the write
path, not usable today. No index on it yet either, since there's nothing
to query by until it's actually populated.

**No retention or archival policy — and deliberately not the seeder's
problem.** Unlike `CacheRefreshQueue` (a working queue — rows are deleted
once processed, so it self-bounds), `AuditTrail` is designed to grow
forever by definition — every audited write adds a row, permanently. The
Bogus seeder does **not** write to it: seeding is bulk test-data
generation, not a real business transaction, so it bypasses `AuditTrail`
the same way it already bypasses `CacheRefreshQueue` (see Orchestration,
above, and "Seeding strategy," below) — otherwise a single seeding run
would instantly create 25,000+ audit rows for writes nobody actually made.
`AuditTrail` only starts accumulating once real API-driven writes happen
post-seed, which keeps its growth tied to actual usage rather than the
one-time data load. No archival plan exists for that ongoing growth
either — accepted as out of scope for this project; worth a partitioning
or archive strategy if this were headed to production.

## Seeding strategy

`Department`/`EquipmentCategory`/`Vendor`/`Employee` are small volume —
plain EF Core `AddRange` + `SaveChanges()` is fine for these. The
constraint is the three high-volume tables: `AcquisitionRequest` (25,000),
`PurchaseOrder`/`Asset` (smaller derived subsets — see below).

**`SqlBulkCopy`, not EF Core, for the high-volume tables.** Naive
per-row `SaveChanges()` doesn't scale cleanly even at this reduced
25,000-row target — EF Core's change tracker cost grows badly past a few
thousand tracked entities per call,
and each `SaveChanges()` is its own round trip. `SqlBulkCopy` streams rows
directly over TDS with no change tracking involved, and needs no new
dependency — `Microsoft.Data.SqlClient` is already implied by the existing
EF Core SQL Server provider.

**Explicit ids, not `IDENTITY` auto-assignment.** `PurchaseOrder` needs
`AcquisitionRequestId` and `Asset` needs `PurchaseOrderId` immediately
after their parent rows are generated — waiting on SQL Server to assign
`IDENTITY` values and querying them back would mean a round trip per
batch at minimum, per row at worst. Instead the seeder assigns sequential
ids itself in C# as it generates the in-memory objects, and bulk-copies
with `SET IDENTITY_INSERT ... ON` around each table's load.

**Batch size**, same lock-escalation-conscious reasoning already used for
the refresh proc and queue drain elsewhere in this doc — `SqlBulkCopy.BatchSize`
set to a few thousand rows per batch, not one 25,000-row bulk operation, to
keep transaction log growth and lock duration bounded.

**Order**, both for FK dependency and the required date ordering
(`RequestDate` → `ApprovedDate`/`RejectedDate` → `OrderDate` →
`AcquiredDate`):
1. `Department` (20), `EquipmentCategory` (4, fixed), `Vendor` (50),
   `Employee` (~750, ~35–40 per department) — reference data first
2. `AcquisitionRequest` (25,000) — bulk-copied with explicit ids; Bogus
   rules enforce `CK_AcquisitionRequest_MutuallyExclusiveDates` at
   generation time via a weighted random split — 60% Approved / 15%
   Rejected / 25% Pending — setting exactly one of
   `ApprovedDate`/`RejectedDate`, or neither
3. `PurchaseOrder` (14,966 measured — the Approved subset of step 2),
   referencing those ids directly since they were assigned in C#, not
   queried back
4. `Asset` (44,956 measured, avg 3.0 units per PO via `Quantity` ∈ [1,5])
   — subset of step 3, same pattern

Phase 2 has actually run against a seeded database — these are measured
counts, not estimates, and reproducible run to run (`Randomizer.Seed` is
fixed). Completed in ~7 seconds end to end, confirming the `SqlBulkCopy`
design's whole premise. Verified directly against the database: exactly
25,000 `AcquisitionRequest` rows, zero `CK_AcquisitionRequest_MutuallyExclusiveDates`
violations, zero orphaned FKs, and the cache rebuilt to exactly 25,000 rows.

**What the seeder does not write:** no `CacheRefreshQueue` signals (already
established — a write outside the API bypasses the queue entirely) and no
`AuditTrail` rows (see `AuditTrail`, above). One call to
`usp_RebuildAllAcquisitionDetailCache` after seeding populates the cache
the direct writes bypassed; `AuditTrail` simply stays empty until real
API-driven writes happen afterward.

## Department Spend Report — the naive-vs-optimized star artifact

```sql
CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSpendReport
    @From         datetime,
    @To           datetime,
    @DepartmentId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Two explicit query shapes, not one (@DepartmentId IS NULL OR ...) catch-all —
    -- the latter would defeat the index seek below even when @DepartmentId is supplied.
    IF @DepartmentId IS NOT NULL
    BEGIN
        SELECT  d.Name AS DepartmentName, ec.Name AS CategoryName,
                COUNT(*) AS RequestCount, SUM(po.TotalCost) AS TotalSpend
        FROM        dbo.AcquisitionRequests r
        INNER JOIN  dbo.PurchaseOrders      po ON po.AcquisitionRequestId = r.Id
        INNER JOIN  dbo.Departments         d  ON d.Id  = r.DepartmentId
        INNER JOIN  dbo.EquipmentCategories ec ON ec.Id = r.EquipmentCategoryId
        WHERE       r.DepartmentId = @DepartmentId
                AND r.RequestDate BETWEEN @From AND @To
        GROUP BY    d.Name, ec.Name
        ORDER BY    ec.Name
        OPTION (RECOMPILE);
    END
    ELSE
    BEGIN
        SELECT  d.Name AS DepartmentName, ec.Name AS CategoryName,
                COUNT(*) AS RequestCount, SUM(po.TotalCost) AS TotalSpend
        FROM        dbo.AcquisitionRequests r
        INNER JOIN  dbo.PurchaseOrders      po ON po.AcquisitionRequestId = r.Id
        INNER JOIN  dbo.Departments         d  ON d.Id  = r.DepartmentId
        INNER JOIN  dbo.EquipmentCategories ec ON ec.Id = r.EquipmentCategoryId
        WHERE       r.RequestDate BETWEEN @From AND @To
        GROUP BY    d.Name, ec.Name
        ORDER BY    d.Name, ec.Name
        OPTION (RECOMPILE);
    END
END
```

**Why `@DepartmentId` exists, and why it isn't optional to the story.** The
first version of this proc only supported the company-wide rollup (every
department, grouped) — measuring it found the composite index
`(DepartmentId, EquipmentCategoryId, RequestDate)` was never seeked,
only scanned, because a `GROUP BY` across every department never filters
by the index's leading columns, only by the trailing `RequestDate`. That's
still a real optimization (narrow index scan vs. wide clustered scan,
below), but it isn't a seek. A department-spend report more realistically
means a manager checking *their own* department, not always a
company-wide rollup — adding that as an explicit, equality-filterable
parameter is both more realistic and the one shape that can actually seek.

**Measured evidence**, department-filtered (`@DepartmentId = 1`), one
fiscal quarter, before vs. after the composite index exists — same query,
same data, index dropped and restored to get a real before/after rather
than a staged one:

| | `AcquisitionRequests` logical reads | Plan operator |
|---|---|---|
| Naive (no index) | 642 | Table Scan |
| Optimized (with index) | 6 | `Index Seek`, `SEEK:([r].[DepartmentId]=(1))` |

**107x fewer logical reads**, and the plan confirms a genuine seek, not
just fewer bytes scanned. Wall-clock timing at this row count (25,000)
is small and noisy either way (tens of ms) — logical reads and the plan
operator are the reliable evidence here, consistent with the reasoning
in `docker-deployment.md`'s note on what evidence actually demonstrates
an optimization versus what's just noise at this scale.

The company-wide rollup (`@DepartmentId = NULL`) still only scans — 642
naive vs. 83 optimized reads (narrow covering-ish index vs. wide clustered
table), a real but different kind of win, worth keeping in the write-up
alongside the seek case rather than in place of it.

## Relationships
```
Department 1───* Employee
Department 1───* AcquisitionRequest *───1 EquipmentCategory
Employee   1───* AcquisitionRequest (RequestedBy)
Employee   1───* AcquisitionRequest (ApprovedBy, nullable)
AcquisitionRequest 1───1 PurchaseOrder *───1 Vendor
PurchaseOrder 1───* Asset *───1 Department

EquipmentAcquisitionDetailCache — standalone, no FKs in either direction
    (values copied from Department, EquipmentCategory, Employee ×2,
     PurchaseOrder, Vendor — a flat snapshot, not a relationship)
```

Two tables sit outside the relationship graph entirely: `MenuItem` (a
different domain) and `EquipmentAcquisitionDetailCache` (the same domain,
flattened). Neither is reachable by a join from the diagram above.

## Resolved
- `ApprovedDate`/`RejectedDate` are mutually exclusive — enforced via
  `CK_AcquisitionRequest_MutuallyExclusiveDates` at the DB level.
- Runs on SQL Server (Docker Compose for local dev — see `database-setup.md`).
- Added `Employee` as a real table rather than a plain `RequestedBy` string,
  wired only into `AcquisitionRequest`'s two employee-FK columns.
- Frontend is React + Material UI, built as separate per-page bundles
  (Webpack, multiple entries) rather than one SPA — see `architecture.md`.
  Scope is full CRUD on `MenuItem` only; the acquisition domain (requests,
  assets, the report) is API-only, exercised via Swagger.
- Added `EquipmentAcquisitionDetailCache` — a standalone, denormalized
  read model with no foreign keys, serving request detail reads as a
  single-key lookup with no joins and no computation. Deliberately *not*
  wired into the department-spend report.
- Cache orchestration — queue table (`CacheRefreshQueue`, storing just
  `AcquisitionRequestId`) written in the same transaction as the business
  write, with resolution done at write time rather than refresh time. A
  `BackgroundService` inside the API polls it; `usp_RefreshAcquisitionDetailCache`
  / `usp_RebuildAllAcquisitionDetailCache` do the rebuild. One branch-free
  code path regardless of what triggered the signal; see Orchestration,
  above, for the column-scoped alternative that was designed and set aside.
- `Status` stays in the cache despite contradicting the "derived, not
  stored" rule on `AcquisitionRequest` — consistent with the table's
  purpose, a read that computes nothing.
- `EquipmentAcquisitionDetailCache` indexing — one composite for the
  grid's mandatory `Department`+`Status`+`Date` filter, plus single-column
  indexes on `EquipmentCategoryId`/`VendorId`/`RequestedByEmployeeId`/
  `ApprovedByEmployeeId` for the refresh proc's resolution queries and
  optional grid filters. See the Indexes table under
  `EquipmentAcquisitionDetailCache`, above, for why these aren't composite
  with each other.
- Grid pagination — `OFFSET`/`FETCH` with a `RequestDate DESC,
  AcquisitionRequestId DESC` stable sort, dynamic `WHERE`/`ORDER BY`
  built per-request from whichever filters and sort column are actually
  applied (never a catch-all `(@Param IS NULL OR Col = @Param)`, which
  would defeat every index above). See "Grid pagination," above.
- `AuditTrail` added — `TableAffected`/`AffectedId`/`Action`/`OldValues`/
  `NewValues`/`DateApplied`, cross-cutting across both domains. `ChangedByEmployeeId`
  scaffolded but unpopulated until the project has auth; not written by
  the seeder, so it only starts accumulating from real API-driven writes.
  See `AuditTrail`, above.
- Seeder batching — `SqlBulkCopy` with explicit, seeder-assigned ids for
  the three high-volume tables, plain EF Core for reference data. See
  "Seeding strategy," above.
- `usp_GetDepartmentSpendReport` — naive-vs-optimized measured for real
  (642 → 6 logical reads, table scan → index seek, department-filtered).
  See "Department Spend Report," above.

## Open — raised by the cache table
- **No timeline slot is budgeted for the orchestration objects** —
  `CacheRefreshQueue`, the two stored procedures, `AuditTrail`'s write-path
  wiring, and the grid's pagination/filter query layer. Design is
  resolved on all of these now; none of it is reflected in
  `project-plan.html`'s existing 12-hour timeline, which still treats the
  cache orchestration as the only remaining API-layer work.
- **`AuditTrail` has no retention/archival plan** for its ongoing,
  real-usage growth — accepted as unbounded for this project's scope; see
  `AuditTrail`, above. (The seeder itself no longer contributes to this —
  see "Seeding strategy.")
- **Deep-page cost on the grid is unmeasured.** `OFFSET`/`FETCH` was
  chosen over keyset pagination on the assumption that the mandatory
  `Department` filter keeps realistic page depths shallow — untested
  against actual seeded data. Keyset (`(RequestDate, AcquisitionRequestId)`
  cursor) is the escalation path if a specific department proves large
  enough for it to matter.
- **A full rebuild must run once after seeding**, since the Bogus seeder
  writes base tables directly and never enqueues signals — the cache
  starts empty until `usp_RebuildAllAcquisitionDetailCache` runs.

## Next
Build the cache orchestration objects — `CacheRefreshQueue`, both stored
procedures, `DetailCacheRefreshWorker` registered via `AddHostedService`,
`AuditTrail` and its write-path wiring — since the design above is now
fixed. Then the Bogus seeding script — 25,000 `AcquisitionRequest` rows
across departments, employees, and a few fiscal years (20 departments, 4
categories, 50 vendors, ~750 employees, ~60/15/25 Approved/Rejected/Pending
split, `PurchaseOrder`/`Asset` derived from the Approved subset — see
"Seeding strategy," above), respecting the mutual-exclusivity rule
and realistic date ordering (RequestDate → ApprovedDate/RejectedDate →
OrderDate → AcquiredDate), batched rather than row-by-row — followed by
one call to `usp_RebuildAllAcquisitionDetailCache` to populate the cache
the seeder bypassed. `MenuItem` gets a small hand-authored seed instead —
the twelve rows under "Seed — five top-level menus" above (Home,
Acquisitions, Assets, Reports, Administration). It's a UI concern, not a
volume table.
