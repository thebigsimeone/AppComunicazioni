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
                var nextRunTime = GetNextRunTime(now);
                var delay = nextRunTime - now;

                _logger.LogInformation($"Il prossimo controllo notifiche sarà alle {nextRunTime}. Attesa per {delay.TotalMinutes:F2} minuti.");

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
                    _logger.LogInformation($"Esecuzione del servizio alle {nextRunTime}...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var monitoringService = scope.ServiceProvider.GetService<IMonitoringService>();
                        var reportService = scope.ServiceProvider.GetService<IReportService>();

                        if (monitoringService == null || reportService == null)
                        {
                            _logger.LogError("Errore: I servizi MonitoringService o ReportService non sono disponibili.");
                            continue;
                        }

                        try
                        {
                            if (nextRunTime.Hour == 10)
                            {
                                _logger.LogInformation("Avvio del controllo notifiche...");
                                await monitoringService.CheckAndSendNotificationsAsync();
                                _logger.LogInformation("Controllo notifiche completato.");
                            }

                            if (nextRunTime.Hour == 17)
                            {
                                _logger.LogInformation("Avvio invio report giornaliero...");
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
                    _logger.LogInformation("Oggi non è un giorno lavorativo. Il servizio non verrà eseguito.");
                }
            }
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            var nextRunMorning = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 0, 0, now.Offset);
            var nextRunEvening = new DateTimeOffset(now.Year, now.Month, now.Day, 17, 0, 0, now.Offset);

            if (now < nextRunMorning) return nextRunMorning;
            if (now < nextRunEvening) return nextRunEvening;

            return nextRunMorning.AddDays(1);
        }

        private bool IsWeekday(DateTimeOffset date)
        {
            return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
        }

    }
}
