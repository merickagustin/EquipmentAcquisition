using System.Data;
using Bogus;
using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Seeding;

public class DatabaseSeeder
{
    private const int DepartmentCount = 20;
    private const int VendorCount = 50;
    private const int EmployeeCount = 750;
    private const int AcquisitionRequestCount = 25_000;
    private const int BulkCopyBatchSize = 2000;

    private static readonly (string Code, string Name)[] DepartmentSeeds =
    {
        ("FIN", "Finance"), ("HR", "Human Resources"), ("IT", "Information Technology"),
        ("MKT", "Marketing"), ("SALES", "Sales"), ("OPS", "Operations"), ("LEGAL", "Legal"),
        ("RD", "Research & Development"), ("PROC", "Procurement"), ("FAC", "Facilities"),
        ("SUPPORT", "Customer Support"), ("ENG", "Engineering"), ("QA", "Quality Assurance"),
        ("LOG", "Logistics"), ("SEC", "Security"), ("COMP", "Compliance"), ("ADMIN", "Administration"),
        ("TRAIN", "Training"), ("PM", "Product Management"), ("BIZDEV", "Business Development")
    };

    private static readonly string[] EquipmentCategorySeeds =
        { "IT Equipment", "Furniture", "Vehicles", "Machinery" };

    private readonly AppDbContext _context;
    private readonly string _connectionString;

    public DatabaseSeeder(AppDbContext context)
    {
        _context = context;
        _connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("AppDbContext has no connection string configured.");
    }

    public async Task SeedAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Not idempotent — Departments is the first table written, so if it already
        // has rows, either a full or partial seed already ran. Fail fast with a clear
        // message instead of a raw SqlException deep in the insert pipeline (this
        // seeder uses explicit ids for the high-volume tables, so a second run would
        // hit primary-key violations regardless of where it happened to fail first).
        if (await _context.Departments.AnyAsync())
        {
            Console.WriteLine("Database already contains seed data — skipping.");
            Console.WriteLine("To reseed from a clean slate: docker compose down -v && docker compose up -d, " +
                "then reapply migrations and re-run --seed.");
            return;
        }

        Randomizer.Seed = new Random(20260830);

        Console.WriteLine("Seeding reference data...");
        var departments = await SeedDepartmentsAsync();
        var categories = await SeedEquipmentCategoriesAsync();
        var vendors = await SeedVendorsAsync();
        var employees = await SeedEmployeesAsync(departments);

        Console.WriteLine($"Generating {AcquisitionRequestCount:N0} acquisition requests...");
        var requests = GenerateAcquisitionRequests(departments, categories, employees);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        Console.WriteLine("Bulk inserting AcquisitionRequest...");
        await BulkInsertAsync(connection, BuildAcquisitionRequestTable(requests), "AcquisitionRequests");

        Console.WriteLine("Generating and bulk inserting PurchaseOrder...");
        var approvedRequests = requests.Where(r => r.ApprovedDate.HasValue).ToList();
        var purchaseOrders = GeneratePurchaseOrders(approvedRequests, vendors);
        await BulkInsertAsync(connection, BuildPurchaseOrderTable(purchaseOrders), "PurchaseOrders");

        Console.WriteLine("Generating and bulk inserting Asset...");
        var assets = GenerateAssets(purchaseOrders, departments);
        await BulkInsertAsync(connection, BuildAssetTable(assets), "Assets");

        Console.WriteLine("Rebuilding EquipmentAcquisitionDetailCache...");
        var command = connection.CreateCommand();
        command.CommandText = "EXEC dbo.usp_RebuildAllAcquisitionDetailCache";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("Seeding MenuItem (hand-authored, not Bogus)...");
        var menuItemCount = await SeedMenuItemsAsync();

        Console.WriteLine($"Done — {departments.Count} departments, {categories.Count} categories, " +
            $"{vendors.Count} vendors, {employees.Count} employees, {requests.Count:N0} requests, " +
            $"{purchaseOrders.Count:N0} purchase orders, {assets.Count:N0} assets, {menuItemCount} menu items.");
    }

    // ===================================================================
    // Reference data — small volume, plain EF Core
    // ===================================================================

    private async Task<List<Department>> SeedDepartmentsAsync()
    {
        var departments = DepartmentSeeds.Select(d => new Department { Code = d.Code, Name = d.Name }).ToList();
        _context.Departments.AddRange(departments);
        await _context.SaveChangesAsync();
        return departments;
    }

    private async Task<List<EquipmentCategory>> SeedEquipmentCategoriesAsync()
    {
        var categories = EquipmentCategorySeeds.Select(n => new EquipmentCategory { Name = n }).ToList();
        _context.EquipmentCategories.AddRange(categories);
        await _context.SaveChangesAsync();
        return categories;
    }

    private async Task<List<Vendor>> SeedVendorsAsync()
    {
        var faker = new Faker<Vendor>()
            .RuleFor(v => v.Name, f => f.Company.CompanyName())
            .RuleFor(v => v.ContactEmail, f => f.Internet.Email());
        var vendors = faker.Generate(VendorCount);
        _context.Vendors.AddRange(vendors);
        await _context.SaveChangesAsync();
        return vendors;
    }

    private async Task<List<Employee>> SeedEmployeesAsync(List<Department> departments)
    {
        var faker = new Faker<Employee>()
            .RuleFor(e => e.FullName, f => f.Name.FullName())
            .RuleFor(e => e.JobTitle, f => f.Name.JobTitle())
            .RuleFor(e => e.DepartmentId, f => f.PickRandom(departments).Id);
        var employees = faker.Generate(EmployeeCount);
        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();
        return employees;
    }

    // Hand-authored, not Bogus — thirteen rows, two levels deep. See table-design.md's
    // "Seed — five top-level menus." Active only where a shell page actually exists
    // (Home, Menu Admin, Vendors, Departments, Equipment Categories) — the rest stay
    // inactive so a reviewer never lands on a 404 from the nav.
    private async Task<int> SeedMenuItemsAsync()
    {
        var home = new MenuItem { Label = "Home", Route = "/", DisplayOrder = 1, IsActive = true };
        var acquisitions = new MenuItem { Label = "Acquisitions", Route = null, DisplayOrder = 2, IsActive = false };
        var assets = new MenuItem { Label = "Assets", Route = null, DisplayOrder = 3, IsActive = false };
        var reports = new MenuItem { Label = "Reports", Route = null, DisplayOrder = 4, IsActive = false };
        var administration = new MenuItem { Label = "Administration", Route = null, DisplayOrder = 5, IsActive = true };

        var topLevel = new[] { home, acquisitions, assets, reports, administration };
        _context.MenuItems.AddRange(topLevel);
        await _context.SaveChangesAsync(); // assigns real Ids, needed as ParentId below

        var children = new[]
        {
            new MenuItem { ParentId = acquisitions.Id, Label = "Requests", Route = "/requests", DisplayOrder = 1, IsActive = false },
            new MenuItem { ParentId = acquisitions.Id, Label = "Purchase Orders", Route = "/purchase-orders", DisplayOrder = 2, IsActive = false },
            new MenuItem { ParentId = assets.Id, Label = "Asset Registry", Route = "/assets", DisplayOrder = 1, IsActive = false },
            new MenuItem { ParentId = reports.Id, Label = "Department Spend", Route = "/reports/department-spend", DisplayOrder = 1, IsActive = false },
            new MenuItem { ParentId = administration.Id, Label = "Menu Admin", Route = "/menu-admin", DisplayOrder = 1, IsActive = true },
            new MenuItem { ParentId = administration.Id, Label = "Vendors", Route = "/vendors", DisplayOrder = 2, IsActive = true },
            new MenuItem { ParentId = administration.Id, Label = "Departments", Route = "/departments", DisplayOrder = 3, IsActive = true },
            new MenuItem { ParentId = administration.Id, Label = "Equipment Categories", Route = "/equipment-categories", DisplayOrder = 4, IsActive = true },
        };
        _context.MenuItems.AddRange(children);
        await _context.SaveChangesAsync();

        return topLevel.Length + children.Length;
    }

    // ===================================================================
    // High-volume tables — generated in memory with explicit sequential
    // ids, bulk-copied. See table-design.md's "Seeding strategy."
    // ===================================================================

    private static List<AcquisitionRequest> GenerateAcquisitionRequests(
        List<Department> departments, List<EquipmentCategory> categories, List<Employee> employees)
    {
        var faker = new Faker();
        var employeesByDept = employees.GroupBy(e => e.DepartmentId).ToDictionary(g => g.Key, g => g.ToList());
        var requests = new List<AcquisitionRequest>(AcquisitionRequestCount);

        for (var id = 1; id <= AcquisitionRequestCount; id++)
        {
            var department = faker.PickRandom(departments);
            var requesterPool = employeesByDept.TryGetValue(department.Id, out var deptEmployees) && deptEmployees.Count > 0
                ? deptEmployees
                : employees;
            var requestDate = faker.Date.Between(DateTime.UtcNow.AddYears(-3), DateTime.UtcNow);
            var quantity = faker.Random.Int(1, 5);

            var request = new AcquisitionRequest
            {
                Id = id,
                DepartmentId = department.Id,
                EquipmentCategoryId = faker.PickRandom(categories).Id,
                RequestedByEmployeeId = faker.PickRandom(requesterPool).Id,
                ItemDescription = faker.Commerce.ProductName(),
                Justification = faker.Random.Bool(0.7f) ? faker.Lorem.Sentence() : null,
                Quantity = quantity,
                EstimatedCost = Math.Round(faker.Random.Decimal(200, 20000), 2),
                RequestDate = requestDate
            };

            // 60% Approved / 15% Rejected / 25% Pending — CK_AcquisitionRequest_MutuallyExclusiveDates
            // is satisfied by construction: exactly one branch sets a date, or neither.
            var roll = faker.Random.Double();
            if (roll < 0.60)
            {
                request.ApprovedDate = requestDate.AddDays(faker.Random.Int(1, 30));
                request.ApprovedByEmployeeId = faker.PickRandom(employees).Id;
            }
            else if (roll < 0.75)
            {
                request.RejectedDate = requestDate.AddDays(faker.Random.Int(1, 30));
                request.RejectionReason = faker.Lorem.Sentence();
            }

            requests.Add(request);
        }

        return requests;
    }

    private static List<PurchaseOrder> GeneratePurchaseOrders(List<AcquisitionRequest> approvedRequests, List<Vendor> vendors)
    {
        var faker = new Faker();
        var purchaseOrders = new List<PurchaseOrder>(approvedRequests.Count);
        var id = 1;

        foreach (var request in approvedRequests)
        {
            var unitCost = Math.Round(request.EstimatedCost / request.Quantity, 2);
            var orderDate = request.ApprovedDate!.Value.AddDays(faker.Random.Int(1, 14));

            purchaseOrders.Add(new PurchaseOrder
            {
                Id = id,
                AcquisitionRequestId = request.Id,
                VendorId = faker.PickRandom(vendors).Id,
                PoNumber = $"PO-{orderDate.Year}-{id:D6}",
                Quantity = request.Quantity,
                UnitCost = unitCost,
                TotalCost = Math.Round(unitCost * request.Quantity, 2),
                OrderDate = orderDate
            });
            id++;
        }

        return purchaseOrders;
    }

    private static List<Asset> GenerateAssets(List<PurchaseOrder> purchaseOrders, List<Department> departments)
    {
        var faker = new Faker();
        var statuses = new[] { AssetStatus.InStock, AssetStatus.Assigned, AssetStatus.Maintenance, AssetStatus.Retired };
        var statusWeights = new[] { 0.30f, 0.55f, 0.10f, 0.05f };
        var assets = new List<Asset>();
        var id = 1;

        foreach (var po in purchaseOrders)
        {
            // One Asset row per physical unit — PurchaseOrder.Quantity can cover a bulk buy.
            for (var unit = 0; unit < po.Quantity; unit++)
            {
                var acquiredDate = po.OrderDate.AddDays(faker.Random.Int(1, 14));
                assets.Add(new Asset
                {
                    Id = id,
                    PurchaseOrderId = po.Id,
                    DepartmentId = faker.PickRandom(departments).Id,
                    AssetTag = $"AST-{id:D6}",
                    SerialNumber = faker.Random.Bool(0.8f) ? faker.Commerce.Ean13() : null,
                    Status = faker.Random.WeightedRandom(statuses, statusWeights),
                    AcquiredDate = acquiredDate,
                    LastUpdated = acquiredDate
                });
                id++;
            }
        }

        return assets;
    }

    // ===================================================================
    // SqlBulkCopy — explicit ids via KeepIdentity, batched
    // ===================================================================

    private static async Task BulkInsertAsync(SqlConnection connection, DataTable table, string destinationTable)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, null)
        {
            DestinationTableName = destinationTable,
            BatchSize = BulkCopyBatchSize,
            BulkCopyTimeout = 120
        };
        foreach (DataColumn column in table.Columns)
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulkCopy.WriteToServerAsync(table);
    }

    private static DataTable BuildAcquisitionRequestTable(List<AcquisitionRequest> requests)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("DepartmentId", typeof(int));
        table.Columns.Add("EquipmentCategoryId", typeof(int));
        table.Columns.Add("RequestedByEmployeeId", typeof(int));
        table.Columns.Add("ItemDescription", typeof(string));
        table.Columns.Add("Justification", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("EstimatedCost", typeof(decimal));
        table.Columns.Add("RequestDate", typeof(DateTime));
        table.Columns.Add("ApprovedDate", typeof(DateTime));
        table.Columns.Add("RejectedDate", typeof(DateTime));
        table.Columns.Add("ApprovedByEmployeeId", typeof(int));
        table.Columns.Add("RejectionReason", typeof(string));

        foreach (var r in requests)
        {
            table.Rows.Add(
                r.Id, r.DepartmentId, r.EquipmentCategoryId, r.RequestedByEmployeeId,
                r.ItemDescription, (object?)r.Justification ?? DBNull.Value, r.Quantity, r.EstimatedCost,
                r.RequestDate, (object?)r.ApprovedDate ?? DBNull.Value, (object?)r.RejectedDate ?? DBNull.Value,
                (object?)r.ApprovedByEmployeeId ?? DBNull.Value, (object?)r.RejectionReason ?? DBNull.Value);
        }

        return table;
    }

    private static DataTable BuildPurchaseOrderTable(List<PurchaseOrder> purchaseOrders)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("AcquisitionRequestId", typeof(int));
        table.Columns.Add("VendorId", typeof(int));
        table.Columns.Add("PoNumber", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("UnitCost", typeof(decimal));
        table.Columns.Add("TotalCost", typeof(decimal));
        table.Columns.Add("OrderDate", typeof(DateTime));

        foreach (var po in purchaseOrders)
        {
            table.Rows.Add(po.Id, po.AcquisitionRequestId, po.VendorId, po.PoNumber,
                po.Quantity, po.UnitCost, po.TotalCost, po.OrderDate);
        }

        return table;
    }

    private static DataTable BuildAssetTable(List<Asset> assets)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("PurchaseOrderId", typeof(int));
        table.Columns.Add("DepartmentId", typeof(int));
        table.Columns.Add("AssetTag", typeof(string));
        table.Columns.Add("SerialNumber", typeof(string));
        table.Columns.Add("Status", typeof(int));
        table.Columns.Add("AcquiredDate", typeof(DateTime));
        table.Columns.Add("LastUpdated", typeof(DateTime));

        foreach (var a in assets)
        {
            table.Rows.Add(a.Id, a.PurchaseOrderId, a.DepartmentId, a.AssetTag,
                (object?)a.SerialNumber ?? DBNull.Value, (int)a.Status, a.AcquiredDate, a.LastUpdated);
        }

        return table;
    }
}
