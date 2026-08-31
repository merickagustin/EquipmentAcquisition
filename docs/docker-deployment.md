# Docker deployment

Three services: `sqlserver` (see `database-setup.md`), `api`, and `web`.
One command brings up the whole stack:

```bash
docker compose up --build
```

| Service | Container port | Host port | Reachable at |
|---|---|---|---|
| sqlserver | 1433 | 1433 | `localhost:1433` (tools only, not the browser) |
| api | 8080 | 8081 | `http://localhost:8081` |
| web | 8080 | 8090 | `http://localhost:8090` — open this one |

## Why the port numbers matter here specifically

`api` and `web` talk to each other two different ways, and each needs a
different address:

- **`api` → `sqlserver`**: server-to-server, inside the Docker network.
  Uses the service name (`Server=sqlserver,1433`) — Docker's internal DNS
  resolves it, the host-mapped port is irrelevant here.
- **Browser → `api`**: the React bundles run in the user's browser, which
  has no idea what the `api` container is called. `PublicApiBaseUrl` and
  `Cors:WebOrigin` are both set to `localhost:<host-port>` values —
  addresses the browser, sitting outside Docker entirely, can actually
  reach. Pointing either of these at the internal service name would work
  for `api`↔`sqlserver` but silently fail here, since the browser can't
  resolve Docker-internal hostnames at all.

This is the same distinction called out in `architecture.md`'s API-base-URL
and CORS sections — Docker is just the place it becomes unavoidable to get
right, since local `dotnet run` masks it (everything's on `localhost` either way).

## Migrations on startup

For this demo, `EquipmentAcquisition.Api` applies pending EF Core
migrations automatically on startup:

```csharp
// Program.cs, after building the app
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}
```

Fine here — single instance, no concurrent-deployment race to worry
about. Worth being able to say out loud that this isn't the pattern for
a real production rollout (a separate migration step ahead of a rolling
deploy, so you're not racing multiple instances against the same
migration), but for a portfolio piece demonstrating the schema end to
end, automatic-on-startup is the right amount of ceremony.

## First-run wait — pull before reading, not after

`mcr.microsoft.com/mssql/server` is ~625MB compressed with no smaller/slim
variant (SQL Server's engine doesn't run on musl libc, so there's no
Alpine build to switch to) — this is inherent to running *real* SQL
Server in a container, the whole reason Docker was chosen over LocalDB in
the first place (see `database-setup.md`). That pull is a genuine
one-time cost on any machine that hasn't run this stack before, reviewers
included, and nothing in the seeding design touches it — it happens
before a single row gets seeded. Measured directly (a clean-slate
`docker compose down -v --rmi all` followed by `up --build`, not a guess):
**10–20+ minutes** on a slow or congested connection. The terminal looks
stalled during this — it isn't; `docker ps`/`docker images` from a second
terminal shows layers still downloading.

The README's first instruction should be `docker compose pull &` (or
`docker compose up -d`, same effect), run **before** anything else —
so the download runs in the background while the reviewer reads the rest
of the README, rather than being pure dead time spent staring at a
progress bar. It doesn't shrink the download; it overlaps it with
something the reviewer needed to do anyway. Cached after the first pull,
so this cost is paid exactly once per machine.

## Not wired in yet

The Bogus seeding script (25,000 `AcquisitionRequest` rows, lowered from
an original 100k+ target — see `table-design.md`'s "Seeding strategy")
and the small hand-authored
`MenuItem` seed aren't part of the container startup — they don't exist
yet (still next on the list). Once they do, the straightforward option is
a `--seed` flag the `api` entrypoint checks for, run once manually
(`docker compose run api dotnet EquipmentAcquisition.Api.dll --seed`)
rather than on every container start.
