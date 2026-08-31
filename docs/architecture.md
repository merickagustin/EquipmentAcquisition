# Architecture — separate API, multi-bundle React frontend

Supersedes the earlier Blazor-based version of this doc. Frontend is
React + Material UI, not Blazor. The API project's role is unchanged —
it's still the only thing that touches `EquipmentAcquisition.Core` /
the DbContext.

```
EquipmentAcquisition.Domain          entities, enums — no dependencies
EquipmentAcquisition.Core            EF Core DbContext, repositories,
                                     services, DTOs, interfaces
EquipmentAcquisition.Api             ASP.NET Web API — owns all DB access
EquipmentAcquisition.Web             ASP.NET Core — serves shell pages + static bundles
EquipmentAcquisition.Tests           xUnit + Moq, targets Core

client/
  shared/                    theme, DTO types, API client, tree helpers,
                             shared MUI components — see inventory below
  nav/                       React+MUI app — the dynamic menu, its own bundle
  home/                      React+MUI app — landing page, conditional on
                             the Requests MenuItem's IsActive flag
  menu-admin/                React+MUI app — MenuItem CRUD, its own bundle
  vendors-admin/              React+MUI app — Vendor CRUD, its own bundle
  departments-admin/          React+MUI app — Department CRUD, its own bundle
  equipment-categories-admin/ React+MUI app — EquipmentCategory CRUD, its own bundle
  requests-admin/              React+MUI app — AcquisitionRequest grid,
                               approve/reject, inline PurchaseOrder
                               management, its own bundle
  assets-admin/                 React+MUI app — Asset paginated CRUD,
                                inline PurchaseOrder lookup, its own bundle
  reports-admin/                React+MUI app — Department Spend Report,
                                read-only, its own bundle
  purchase-orders-admin/        React+MUI app — PurchaseOrder paginated
                                CRUD, inline AcquisitionRequest lookup,
                                its own bundle
```

## Backend layering: Application and Infrastructure are one project

Earlier drafts split these into `EquipmentAcquisition.Application` (DTOs,
service interfaces) and `EquipmentAcquisition.Infrastructure` (DbContext,
repositories). They are now a single `Core` project. The reasoning, since
the four-layer version is the more conventional diagram and the merge
should read as a decision rather than an omission:

- **This is not a microservice architecture, and there is one database.**
  The split's usual payoff — independently deployable pieces, or a
  swappable persistence provider — has nothing to attach to here.
- **Provider portability would be fiction.** The centrepiece of this
  project is a SQL Server stored procedure tuned against SQL Server
  execution plans. There is no scenario where the data layer gets swapped,
  so an abstraction defending that possibility defends nothing.
- **Testability does not depend on assembly boundaries.** Moq mocks
  interfaces, not projects. `MenuItemService` depending on
  `IMenuItemRepository` is equally mockable whether the implementation
  lives one project over or one folder over. `Core` keeps its interfaces,
  so the xUnit suite is unaffected by the merge.

**What the merge gives up**, stated plainly: the compiler no longer
prevents a service from bypassing its repository and taking `AppDbContext`
directly. In a split solution that's a build error; here it's a
convention. The rule that replaces it — **nothing under `Services/` takes
`AppDbContext` in its constructor, only repository interfaces** — is a
review check, not a compile check. If `AppDbContext` shows up there, the
merge has started to rot.

```
EquipmentAcquisition.Core/
  Data/            AppDbContext, entity configurations, Migrations/
  Repositories/    IMenuItemRepository      + MenuItemRepository
                   IReportRepository        + ReportRepository   (FromSqlRaw → proc)
                   IDetailCacheRepository   + DetailCacheRepository
  Services/        IMenuItemService + MenuItemService
                   IReportService   + ReportService
  Dtos/            MenuItemDto, ReportRowDto, RequestDetailDto
```

`Domain` stays its own project because it costs nothing — entities and
enums with no package references at all, which keeps EF-facing
configuration out of the business model and gives `Tests` something to
reference without dragging EF Core in.

Two repositories here are deliberately thin, and shouldn't be dressed up:
`ReportRepository` is one `FromSqlRaw` call to a named stored procedure,
and `DetailCacheRepository` is a single-key lookup against a standalone
table with no joins (see `table-design.md`). Both get an interface so the
service above them can be tested with a mock — that is the entire reason,
and it's enough of one.

## The pattern: one bundle per page, not one SPA

This is a direct rebuild of the real legacy setup — a `Web.sitemap` +
`asp:Menu` driving hardcoded navigation, with each `.aspx` page loading
its own `app.js` — just with the hardcoded parts replaced by data. It is
**not** a single React Router SPA with one bundle and client-side routes.

- **`EquipmentAcquisition.Web`** serves a shell page per feature area
  (Razor Pages, one `.cshtml` per page). Each shell:
  - references the shared `nav` bundle, mounted into `<div id="nav-root">`
    — present on every shell page via a common `_Layout.cshtml`
  - references that page's own bundle, mounted into `<div id="content-root">`
- Clicking a menu item is a **normal navigation** to that page's URL
  (`/menu-admin`, `/vendors`, `/departments`, `/equipment-categories`,
  `/requests`, `/assets`, `/reports/department-spend`) — the browser loads
  a new shell, which loads that page's own bundle. No client-side router
  deciding what to render; the server decides which shell (and therefore
  which bundle) to serve, same as the old `.aspx`-per-page setup.
- Each bundle is a fully independent React app — its own `ReactDOM.createRoot`
  call, its own state. They share nothing at runtime except the DOM they're
  both mounted into (nav in one div, page content in another) and,
  independently, the MUI theme module they both import from `client/shared`.

Ten bundles exist today, one per sidebar entry: `nav` (the dynamic menu
itself), `home` (the landing page — see "Home: reading a MenuItem flag
outside the nav" below), `menu-admin` (MenuItem CRUD), `vendors-admin` /
`departments-admin` / `equipment-categories-admin` (reference-data CRUD,
same list+dialog shape as `menu-admin` minus the tree), `requests-admin`
(a paginated, filtered grid over `AcquisitionRequest` with an
approve/reject workflow and inline `PurchaseOrder` management — see
"Acquisition Requests: a second shape" below), `assets-admin` (paginated
CRUD over `Asset`, with an inline Purchase-Order-id lookup instead of a
picker), `reports-admin` (the read-only Department Spend Report — no
CRUD, no `FormDialog`, just filters and a table), and
`purchase-orders-admin` (paginated CRUD over `PurchaseOrder` itself, with
an inline Acquisition-Request-id lookup — a second, standalone entry
point onto the same rows `requests-admin` manages per-row).

## Build: Webpack, multiple entries

Chosen deliberately over Vite here (the trade-blotter take-home already
uses Vite) — Webpack is what the real legacy React frontend was actually
built with, so the tooling itself is part of the parallel being drawn.

```js
// webpack.config.js — see the file itself for the full, current entry list
module.exports = {
  entry: {
    nav: './nav/index.tsx',
    'menu-admin': './menu-admin/index.tsx',
    // 'vendors-admin', 'departments-admin', 'equipment-categories-admin',
    // 'requests-admin', 'assets-admin', 'reports-admin',
    // 'purchase-orders-admin' follow the same one-entry-per-page pattern.
    // Named *-admin, not e.g. 'vendors' — an entry named 'vendors' would
    // collide with the splitChunks cacheGroup below, which already emits
    // vendors/app.js.
  },
  output: {
    filename: '[name]/app.js',
    path: path.resolve(__dirname, 'dist'),   // client/dist — see note below on why not straight into wwwroot
  },
  optimization: {
    // A single, deterministically-named vendor chunk (not per-entry auto-hashed
    // names) — Razor references it with a plain <script> tag, no manifest needed.
    splitChunks: {
      cacheGroups: { vendors: { test: /[\\/]node_modules[\\/]/, name: 'vendors', chunks: 'all' } },
    },
  },
};
```

`splitChunks` matters here specifically because there are multiple
entries sharing the same big dependencies (React, MUI) — without it,
every bundle pays the full React+MUI weight on its own.

**Output goes to `client/dist`, not directly into the Web project's
`wwwroot`.** That decoupling is what makes the Docker build work cleanly
(see `docker-deployment.md`) — the client build runs in its own container
stage with no knowledge of the .NET project layout, and its output gets
copied into `wwwroot/dist` as a separate step. Locally, `EquipmentAcquisition.Web`
serves straight from `client/dist` in Development via a second static-file
provider pointed at that path, so `npm run build -- --watch` picks up
instantly without a copy step getting in the way; in Production (and in
the container) it serves `wwwroot/dist` normally, where the Dockerfile
already placed the built bundles.

## API base URL — runtime-configured, not baked in

The client can't hardcode the API's URL at build time — it's different
between local dev and Docker, and baking it in would mean rebuilding the
image for every environment. Instead, `EquipmentAcquisition.Web` injects
it into the page server-side, read from its own configuration:

```cshtml
@* _Layout.cshtml *@
<script>window.__API_BASE_URL__ = "@Configuration["PublicApiBaseUrl"]";</script>
```

```ts
// client/shared/apiClient.ts
const baseUrl = (window as any).__API_BASE_URL__ as string;
```

`PublicApiBaseUrl` is set via `appsettings.Development.json` locally and
via an environment variable in Docker — and it has to be a URL the
**browser** can reach (the host-mapped port), not an internal Docker
network hostname, since the fetch calls run client-side. Same reasoning
applies to the CORS origin below.

## UI design

**Shell layout** — a top MUI `AppBar` (app title) plus a persistent left
`Drawer` (~240px) hosting `nav`; content area to the right renders
whatever bundle the current shell page mounts. The `AppBar`/`Drawer`
chrome itself lives in `_Layout.cshtml` (plain HTML/CSS, not React) —
`nav-root` sits inside the drawer, `content-root` beside it.

**`nav` rendering** — a sidebar tree, not a horizontal dropdown menu.
Top-level items are direct links; group headers (`Route = null`) expand
and collapse on click to reveal children. The current route is
highlighted. Chosen over a top nav bar specifically because it handles
the seed data's nesting (`Acquisitions → Requests`, `Administration →
Menu Admin`, etc.) without needing dropdown-within-dropdown behavior.

**`menu-admin` listing** — a plain MUI `Table`, not a `DataGrid` or a
tree-view control. Rows are indented by tree depth (reusing the same
depth calculation `nav` uses to build its tree — see `menuTree.ts` below).
Columns: Label, Route, Display Order, an inline `Switch` for `IsActive`
(toggles without opening `FormDialog` — this is what makes the "toggle
Reports, reload, watch nav grow" demo fast to perform), Edit/Delete icon
buttons. A plain table over a heavier tree-view widget was a deliberate
scope call, same reasoning as everywhere else in this project: simplest
thing that does the job, not the most visually elaborate one.

**Create/Edit form** (rendered inside the shared `FormDialog`): Label
(text), Route (text, optional — helper text "leave blank for a group
header"), Parent (dropdown of existing items, "— top level —" as the null
option), Display Order (number), Active (checkbox).

**`vendors-admin` / `departments-admin` / `equipment-categories-admin`** —
the same plain-`Table` + `FormDialog` + `ConfirmDialog` shape as
`menu-admin`, minus the tree: no indentation, no parent picker, since
these are flat reference data. Proof that the pattern generalizes with
no changes to the shared components themselves — only the fields inside
`FormDialog`'s children differ per entity.

**`requests-admin`** is a different shape, not a fourth copy of the same
pattern — see "Acquisition Requests: a second shape" below.

**Theme** — a standard MUI palette (blue primary, light background,
default Roboto typography), no custom branding. This is a portfolio
piece demonstrating the architecture, not a themed product.

## Acquisition Requests: a second shape

`requests-admin` earns its own section because it isn't just "another
`FormDialog` consumer" — it reads through a different data path and
carries real async-consistency behavior that the reference-data pages
don't have to deal with.

**It reads the cache, not the base table.** The list is
`GET /api/acquisition-requests/grid`, backed by
`EquipmentAcquisitionDetailCache` (see `table-design.md`), not
`AcquisitionRequests` directly. `DepartmentId`, `Status`, and a `From`/`To`
date range are mandatory query parameters — they match the cache's
mandatory composite index, and the API rejects a request missing any of
them with a `400` rather than silently defaulting into a bad query
(`RequestListQuery`'s fields are nullable specifically so "not supplied"
is distinguishable from "supplied as zero/default").

**Mutations don't reflect immediately.** A write lands in the real table
at once, but the cache only catches up when `DetailCacheRefreshWorker`
drains its queue — up to ~2 seconds later (see `table-design.md`'s
orchestration section). Rather than guess at a delay with a timer, the UI
says so: every mutation (create/edit, approve, reject, delete, purchase
order create/edit/remove) shows a `Snackbar` notice — *"Request created —
it can take a few seconds to appear below."* — with its own Refresh
action, plus a persistent Refresh button in the page header. The delay is
disclosed, not masked.

**Purchase Orders started with no page of its own, and now has two entry
points** — a deliberate reversal, not scope creep by accident. `PurchaseOrder`
has a unique FK on `AcquisitionRequestId` — 1:0-or-1, enforced at the DB
level — so an Approved request's row in `requests-admin` grows a
shopping-cart action that opens a `FormDialog` for creating, editing, or
removing *its* linked PO, without ever fetching the full `PurchaseOrders`
table. That dialog needs the PO's own id and full field set, which the
grid row doesn't carry (it only has `VendorName`/`TotalCost`, enough to
render the column, not enough to edit) — so opening it calls
`GET /api/purchase-orders/by-request/{acquisitionRequestId}`, a lookup
added specifically to back this. It returns `200` with a `null` body when
no PO exists yet, not `404` — that's a normal state for an Approved
request, not an error.

`purchase-orders-admin` is the second entry point: a standalone paginated
list at `/purchase-orders`, backed by its own `GET /api/purchase-orders/grid`
(optional Vendor/AcquisitionRequestId filters — like `AssetListQuery`, not
`RequestListQuery`, since PurchaseOrders has no multi-join read model or
mandatory-triad index to protect). Creating one here takes a raw
Acquisition Request id with the same inline-lookup pattern `assets-admin`
uses for Purchase Order ids — confirms the request is Approved before you
commit, since there's no bounded picker list of eligible requests. The two
pages aren't redundant: `requests-admin`'s action is the fast path while
you're already looking at a specific request; `purchase-orders-admin` is
for browsing/filtering POs on their own terms (by vendor, say) without
first finding the request that spawned each one.

## Home: reading a MenuItem flag outside the nav

`home` is the one bundle whose content is conditional on something other
than its own data. On mount it fetches `/api/menu-items` (the same call
`nav` makes) and checks whether the Requests entry — the row with
`route === '/requests'` — is `isActive`. Only if it is does it fetch
`GET /api/acquisition-requests/pending-by-department` and render the
Pending Requisitions by Department widget; otherwise it renders a plain
note explaining why the widget isn't there and how to turn it on.

The point isn't the widget — it's that `IsActive` already means "this
page exists and is reachable," which is exactly the condition that should
gate *any* content referencing that page, not just its own nav entry.
Toggling Requests off in Menu Admin doesn't just remove a sidebar link;
it also removes anything else that assumed that page was live. This is
the same `MenuItem` flag doing double duty, not a second flag invented
for the Home page specifically — one source of truth for "is this feature
on," read from two different bundles for two different purposes.

`GetPendingCountsByDepartmentAsync` (backing that endpoint) is worth a
note in its own right: it returns *every* Department, including
zero-pending ones, via a correlated `Count()` subquery per department
rather than a `GROUP BY` over the cache — a `GROUP BY` would silently
drop any department with nothing pending, which is a real dashboard bug
(a quiet department reads as "no data" instead of "genuinely nothing
pending"). It also can't be ordered as part of the same query — EF Core
won't translate an `ORDER BY` on a property of a `record` constructed
from a correlated subquery — so results are materialized first and sorted
client-side. Fine at 20 departments; would need revisiting past a few
hundred.

## Shared theme, independent apps

```ts
// client/shared/theme.ts
export const theme = createTheme({ /* palette, typography */ });
```

```tsx
// client/nav/index.tsx
createRoot(document.getElementById('nav-root')!).render(
  <ThemeProvider theme={theme}><NavApp /></ThemeProvider>
);

// client/menu-admin/index.tsx — separate entry, separate root, same theme import
createRoot(document.getElementById('content-root')!).render(
  <ThemeProvider theme={theme}><MenuAdminApp /></ThemeProvider>
);
```

## `client/shared` — inventory

Everything both bundles are allowed to know about. Nothing here may import
from `client/nav` or `client/menu-admin`; the dependency runs one way only.

```
client/shared/
  theme.ts                   MUI createTheme — palette, typography
  types.ts                   TS shapes mirroring the API's DTOs (MenuItemDto, …)
  apiClient.ts               fetch wrapper over window.__API_BASE_URL__
                             + toErrorMessage(response) → user-facing text
  menuTree.ts                buildTree(flat) / flattenWithDepth(tree)
  components/
    FormDialog.tsx           title + form body + Cancel/Save, one shape for every form
    ConfirmDialog.tsx        destructive confirm — deletes
```

**What started as an investment has since paid off.** `theme.ts`,
`apiClient.ts`, and `types.ts` are used by all six bundles; `menuTree.ts`'s
tree-building is used by `nav` and `menu-admin` specifically (the
reference-data and requests pages are flat lists, no tree to build).
`FormDialog` and `ConfirmDialog`, originally written for `menu-admin`
alone with no second consumer yet, are now used by every CRUD bundle —
`vendors-admin`, `departments-admin`, `equipment-categories-admin`,
`assets-admin`, `purchase-orders-admin`, and `requests-admin` (five
separate forms in that one bundle alone: create/edit request, approve,
reject, and the purchase-order dialog) all render the same two
components. `reports-admin` is the one exception — read-only, no form, no
dialog, nothing to plug in. Nothing in `FormDialog.tsx` or
`ConfirmDialog.tsx` changed to make any of this possible — the contract
held.

**`FormDialog` standardises three things**, and the third is the one that
usually gets missed:

1. **Layout** — title/content/actions spacing, width, button order and
   emphasis.
2. **Behaviour** — rendering the dialog's `Paper` as a `form` element
   makes **Enter submit**, which is trivially easy to forget per-dialog
   and jarring when only some screens have it. Also: actions disabled
   while submitting, no close on backdrop click, no close mid-flight.
3. **Error presentation** — one `Alert`, one place, fed by
   `toErrorMessage`. Without a shared mapping, every screen invents its
   own wording for the same `409` and uniformity stops at the visuals.
   The mapping the API actually produces: `400` → validation text,
   `409` → conflict (delete-with-children, reparent cycle), `404` →
   "no longer exists".

**The actual shared contract** — `FormDialog` owns the dialog chrome and
behavior; the caller supplies only the form fields and the submit
handler. This is what makes it a genuine template rather than a
MenuItem-specific component wearing a generic name:

```tsx
// client/shared/components/FormDialog.tsx
interface FormDialogProps {
  open: boolean;
  title: string;
  onClose: () => void;
  onSubmit: () => void | Promise<void>;
  submitting: boolean;
  error?: string | null;
  submitLabel?: string;        // default "Save"
  children: React.ReactNode;   // the form fields — entity-specific
}
```

`menu-admin`'s Create and Edit both render the *same* `FormDialog`
instance shape, differing only in `title`, initial field values, and
`onSubmit`:

```tsx
<FormDialog
  open={dialogOpen}
  title={editing ? 'Edit Menu Item' : 'New Menu Item'}
  onSubmit={handleSubmit}
  submitting={isSaving}
  error={errorMessage}
  onClose={closeDialog}
>
  <TextField label="Label" value={label} onChange={...} required />
  <TextField label="Route" value={route} onChange={...}
             helperText="Leave blank for a group header" />
  <Select label="Parent" value={parentId} onChange={...}>
    <MenuItem value="">— top level —</MenuItem>
    {/* existing items */}
  </Select>
  <TextField type="number" label="Display Order" value={displayOrder} onChange={...} />
  <FormControlLabel control={<Checkbox checked={isActive} onChange={...} />} label="Active" />
</FormDialog>
```

Every other admin form plugs into this exact same `FormDialog` the same
way — new fields as children, a new `onSubmit`, nothing else changes.
`VendorsAdminApp`, `DepartmentsAdminApp`, and `EquipmentCategoriesAdminApp`
each render it with a two- or three-field form; `RequestsAdminApp` renders
it five separate times (create/edit, approve, reject, purchase order)
with entirely different field sets, including an `Autocomplete` for
employee pickers instead of a plain `TextField`. None of that required
touching `FormDialog.tsx` itself. Any future page (Asset Registry, say)
plugs in the same way.

`splitChunks: { chunks: 'all' }` (above) already emits these into a common
chunk, so a module imported by both bundles is downloaded once. No extra
configuration.

**Version note:** rendering the dialog paper as a form is
`PaperProps={{ component: 'form', onSubmit }}` on MUI v5, and
`slotProps={{ paper: { … } }}` on v6+, where `PaperProps` is deprecated.
Pin the MUI major deliberately so the component doesn't emit warnings.

## CORS — this is the one thing that flips from the Blazor version

Blazor Server's calls to the API happened server-to-server, so CORS
never came up. React runs **in the browser**, so `fetch()` calls from
`EquipmentAcquisition.Web`'s origin to `EquipmentAcquisition.Api`'s
origin are cross-origin and the API needs an explicit CORS policy:

```csharp
// EquipmentAcquisition.Api/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", p => p
        .WithOrigins(builder.Configuration["Cors:WebOrigin"]!)   // browser-facing Web origin — differs per environment
        .AllowAnyHeader()
        .AllowAnyMethod());
});
// ...
app.UseCors("Web");
```

`Cors:WebOrigin` is `https://localhost:7090` in local dev config, and set
via environment variable (`Cors__WebOrigin`) in Docker Compose — see
`docker-deployment.md`.

## MenuItem API surface (unchanged from the Blazor version)

| Method | Route | Notes |
|---|---|---|
| GET | `/api/menu-items` | Flat list, ordered by `ParentId`, `DisplayOrder` |
| POST | `/api/menu-items` | Create |
| PUT | `/api/menu-items/{id}` | Update |
| DELETE | `/api/menu-items/{id}` | Returns `409 Conflict` if the item has children — checked in the API before the DB's `Restrict` constraint would reject it |

The API still returns a **flat** list rather than a pre-built tree —
the `nav` bundle builds the parent/child tree client-side (TypeScript
this time, not C#), which is the direct parallel to the real problem:
flattened menu table → rendered nav tree. That transform is worth
keeping visible in the code either way; which language it's written in
doesn't change why it's there.

## Running locally

Three processes:

```bash
dotnet run --project src/EquipmentAcquisition.Api    # e.g. https://localhost:7080
dotnet run --project src/EquipmentAcquisition.Web     # e.g. https://localhost:7090
npm run build --prefix client -- --watch              # webpack, watch mode
```
