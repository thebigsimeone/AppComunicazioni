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

                if (nextRunTime <= now)
                {
                    _logger.LogWarning($"Avvio del servizio dopo l'orario pianificato. Recupero task mancati.");
                    await ExecuteTasks(nextRunTime, stoppingToken);
                    nextRunTime = GetNextRunTime(DateTimeOffset.Now); // Aggiorna il prossimo ciclo
                }

                var delay = nextRunTime - DateTimeOffset.Now;
                _logger.LogInformation($"Ora corrente: {now}. Prossima esecuzione pianificata alle {nextRunTime}. Attesa per {delay.TotalMinutes:F2} minuti.");

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("Servizio di background interrotto. Task cancellato.");
                    break;
                }

                await ExecuteTasks(nextRunTime, stoppingToken);
            }
        }

        private async Task ExecuteTasks(DateTimeOffset runTime, CancellationToken stoppingToken)
        {
            if (IsWeekday(runTime))
            {
                _logger.LogInformation($"Esecuzione del servizio alle {runTime} (giorno lavorativo).");

                using var scope = _serviceProvider.CreateScope();
                var monitoringService = scope.ServiceProvider.GetService<IMonitoringService>();
                var reportService = scope.ServiceProvider.GetService<IReportService>();

                if (monitoringService == null || reportService == null)
                {
                    _logger.LogError("Uno dei servizi necessari non è disponibile.");
                    return;
                }

                try
                {
                    if (runTime.Hour == 10)
                    {
                        _logger.LogInformation("Avvio del controllo notifiche delle 10:00...");
                        await monitoringService.CheckAndSendNotificationsAsync();
                        _logger.LogInformation("Controllo notifiche completato.");
                    }

                    if (runTime.Hour == 17)
                    {
                        _logger.LogInformation("Avvio del report giornaliero delle 17:00...");
                        await reportService.GenerateAndSendDailyReportAsync();
                        _logger.LogInformation("Report giornaliero inviato correttamente.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante l'esecuzione del servizio alle {runTime}: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning($"Il giorno {runTime:yyyy-MM-dd} non è un giorno lavorativo. Nessuna azione eseguita.");
            }
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            var nextRunMorning = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 0, 0, now.Offset);
            var nextRunEvening = new DateTimeOffset(now.Year, now.Month, now.Day, 17, 0, 0, now.Offset);

            if (now < nextRunMorning)
                return nextRunMorning; // Prossima esecuzione alle 10:00
            if (now < nextRunEvening)
                return nextRunEvening; // Prossima esecuzione alle 17:00

            return nextRunMorning.AddDays(1); // Prossima esecuzione alle 10:00 del giorno successivo
        }

/*        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
          {
              // Esegui ogni minuto per test
              return now.AddSeconds(30);  // Esegue ogni 30 secondi
          }*/

        private bool IsWeekday(DateTimeOffset date)
        {
            return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
        }
    }
}
