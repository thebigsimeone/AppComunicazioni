public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(IServiceProvider serviceProvider, ILogger<CleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var cleanupService = scope.ServiceProvider.GetRequiredService<CleanupService>();
                try
                {
                    await cleanupService.CleanupOldRecordsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante la pulizia: {ex.Message}");
                }
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken); // Attende un giorno prima di rieseguire
        }
    }
}
