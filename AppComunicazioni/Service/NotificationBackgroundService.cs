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

                // Calcola quanto manca al prossimo ciclo di esecuzione alle 10:00
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

                if (IsWeekday(nextRunTime))
                {
                    _logger.LogInformation("Esecuzione periodica del controllo notifiche e report...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
                        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

                        try
                        {
                            // Controllo notifiche alle 10:00
                            if (nextRunTime.Hour == 10)
                            {
                                await monitoringService.CheckAndSendNotificationsAsync();
                                _logger.LogInformation("Controllo notifiche completato.");
                            }

                            // Invio report alle 18:00
                            if (nextRunTime.Hour == 18)
                            {
                                await reportService.GenerateAndSendDailyReportAsync();
                                _logger.LogInformation("Report giornaliero inviato.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Errore durante l'esecuzione del servizio di background: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Oggi non è un giorno lavorativo, il servizio non verrà eseguito.");
                }
            }
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            // Esecuzioni programmate alle 10:00 e 18:00
            var nextRunMorning = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 0, 0, now.Offset);
            var nextRunEvening = new DateTimeOffset(now.Year, now.Month, now.Day, 18, 0, 0, now.Offset);

            if (now < nextRunMorning)
            {
                return nextRunMorning;
            }
            else if (now < nextRunEvening)
            {
                return nextRunEvening;
            }
            else
            {
                return nextRunMorning.AddDays(1);
            }
        }

        private bool IsWeekday(DateTimeOffset date)
        {
            return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
        }
    }
}
