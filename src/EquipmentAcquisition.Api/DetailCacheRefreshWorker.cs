using EquipmentAcquisition.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Api;

/// <summary>Polls CacheRefreshQueue on a 2s timer and drains it via
/// usp_RefreshAcquisitionDetailCache. Lives inside the API process — starts
/// and stops with it. See table-design.md's Orchestration section.</summary>
public class DetailCacheRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DetailCacheRefreshWorker> _logger;

    public DetailCacheRefreshWorker(IServiceScopeFactory scopeFactory, ILogger<DetailCacheRefreshWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.ExecuteSqlRawAsync("EXEC dbo.usp_RefreshAcquisitionDetailCache", stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DetailCacheRefreshWorker tick failed");
            }
        }
    }
}
