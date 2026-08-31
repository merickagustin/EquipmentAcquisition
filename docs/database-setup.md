# Database — SQL Server

Runs via Docker so anyone cloning the repo can spin it up with one command —
no local SQL Server install required for a reviewer to run the project.

```bash
docker compose up -d
```

Connects on `localhost:1433`, SA password `YourStrong!Passw0rd` (dev-only,
fine to commit — never a real credential). Swap in a local SQL Server
instance or LocalDB instead by just changing the connection string; nothing
in the entity/DbContext design is Docker-specific.

## NuGet package

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

## Connection string (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=EquipmentAcquisitionDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  }
}
```

## DI registration (Program.cs)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

## What this changes from the provider-neutral draft

Nothing in the entity classes — `decimal(18,2)`, `HasMaxLength(...)` are
already SQL Server-idiomatic and map cleanly. Two things specific to SQL
Server worth using deliberately, since they're part of the story:

- **The optimized report query becomes a real `CREATE PROCEDURE`**, called
  via a raw `SqlCommand`/`ExecuteReader` (or EF Core's `FromSqlRaw`) rather
  than translated LINQ — this is closer to what you'd actually reach for at
  work when a LINQ-generated query doesn't optimize well.
- **Execution plans** (SSMS or Azure Data Studio → "Include Actual Execution
  Plan") give you the concrete "index scan → index seek" before/after
  evidence for the write-up, not just wall-clock timing.

## Still open
Same question as before — are `ApprovedDate`/`RejectedDate` on
`AcquisitionRequest` mutually exclusive? Needed before the seeder runs.
