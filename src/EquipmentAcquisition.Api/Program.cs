using EquipmentAcquisition.Api;
using EquipmentAcquisition.Api.Middleware;
using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Seeding;
using EquipmentAcquisition.Core.Services;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ICacheRefreshQueueRepository, CacheRefreshQueueRepository>();
builder.Services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();

builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEquipmentCategoryRepository, EquipmentCategoryRepository>();
builder.Services.AddScoped<IEquipmentCategoryService, EquipmentCategoryService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IAcquisitionRequestRepository, AcquisitionRequestRepository>();
builder.Services.AddScoped<IAcquisitionRequestService, AcquisitionRequestService>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IDetailCacheRepository, DetailCacheRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddHostedService<DetailCacheRefreshWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", p => p
        .WithOrigins(builder.Configuration["Cors:WebOrigin"]!)   // browser-facing Web origin — differs per environment
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Applies pending migrations automatically on startup. Fine here — single
// instance, no concurrent-deployment race to worry about; not the pattern
// for a real production rollout (a separate migration step ahead of a
// rolling deploy). See docker-deployment.md.
using (var migrationScope = app.Services.CreateScope())
{
    migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await new DatabaseSeeder(context).SeedAsync();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Web");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
