using AppComunicazioni.Interface;

namespace AppComunicazioni.Service
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;

                // Calcola quanto manca al prossimo ciclo di esecuzione alle 9:00 del mattino
                var nextRunTime = GetNextRunTime(now);
                var delay = nextRunTime - now;

                _logger.LogInformation($"Il prossimo controllo notifiche sarà alle {nextRunTime}. Attesa per {delay.TotalMinutes} minuti.");


                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Servizio di background cancellato.");
                    break;
                }

                // Verifica se è un giorno lavorativo (dal lunedì al venerdì)
                if (IsWeekday(nextRunTime))
                {
                    _logger.LogInformation("Esecuzione periodica del controllo notifiche...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
                        try
                        {
                            await monitoringService.CheckAndSendNotificationsAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Errore durante il controllo notifiche: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Il giorno non è lavorativo, il controllo notifiche non verrà eseguito.");
                }
            }
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            // Imposta la prossima esecuzione alle 10:00 del mattino
            var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 00, 0, now.Offset);

            // Se sono passate le 9:00 di oggi, sposta la prossima esecuzione a domani
            if (now >= nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun;
        }

        private bool IsWeekday(DateTimeOffset date)
        {
            // Controlla se il giorno è dal lunedì al venerdì
            return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
        }
    }
}
