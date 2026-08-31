using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Dev only: serve straight from client/dist so `npm run build` (or a future
// --watch) is picked up instantly with no copy step. In Production/Docker,
// the Dockerfile places the built bundles into wwwroot/dist instead, and the
// UseStaticFiles() call above already serves that. See architecture.md.
// The directory check (not just IsDevelopment()) matters because the Docker
// image also runs ASPNETCORE_ENVIRONMENT=Development but has no repo checkout
// alongside it — only a real local `dotnet run` has client/dist at this path.
var clientDistPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "client", "dist");
if (app.Environment.IsDevelopment() && Directory.Exists(clientDistPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(clientDistPath)),
        RequestPath = "/dist",
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
