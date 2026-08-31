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
  menu-admin/                React+MUI app — MenuItem CRUD, its own bundle
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
  (`/menu-admin`, later `/requests`, `/assets`, `/reports`) — the browser
  loads a new shell, which loads that page's own bundle. No client-side
  router deciding what to render; the server decides which shell (and
  therefore which bundle) to serve, same as the old `.aspx`-per-page setup.
- Each bundle is a fully independent React app — its own `ReactDOM.createRoot`
  call, its own state. They share nothing at runtime except the DOM they're
  both mounted into (nav in one div, page content in another) and,
  independently, the MUI theme module they both import from `client/shared`.

This is scoped to two bundles for now, matching "only menus": `nav` (the
dynamic menu itself) and `menu-admin` (MenuItem CRUD). Additional pages
(requests, assets, the report) would each get their own bundle later,
following the same pattern — nothing about the architecture is specific
to menus, that's just the one slice being built.

## Build: Webpack, multiple entries

Chosen deliberately over Vite here (the trade-blotter take-home already
uses Vite) — Webpack is what the real legacy React frontend was actually
built with, so the tooling itself is part of the parallel being drawn.

```js
// webpack.config.js
module.exports = {
  entry: {
    nav: './client/nav/index.tsx',
    'menu-admin': './client/menu-admin/index.tsx',
  },
  output: {
    filename: '[name]/app.js',
    path: path.resolve(__dirname, 'dist'),   // client/dist — see note below on why not straight into wwwroot
  },
  optimization: {
    splitChunks: { chunks: 'all' }, // shared React/MUI vendor chunk across bundles
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

**Theme** — a standard MUI palette (blue primary, light background,
default Roboto typography), no custom branding. This is a portfolio
piece demonstrating the architecture, not a themed product.

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

**What is actually shared today, and what is an investment.** `theme.ts`,
`types.ts`, `apiClient.ts` and `menuTree.ts` are used by *both* bundles
right now — `nav` builds the tree to render it, `menu-admin` uses the same
depth calculation to indent its grid, and both fetch `MenuItemDto` through
the same client. The two dialogs are used by `menu-admin` only, since
`nav` opens nothing. They are shared for the pages that don't exist yet
(requests, assets, vendors), which is a deliberate bet on this
architecture being extended, not a saving being realised now. Worth about
half an hour, not more.

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

Any future form (`Vendor` edit, `Department` edit, whichever page gets
built next) plugs into this exact same `FormDialog` — new fields as
children, a new `onSubmit`, nothing else changes. That reuse is the whole
point of the half-hour investment noted above.

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
