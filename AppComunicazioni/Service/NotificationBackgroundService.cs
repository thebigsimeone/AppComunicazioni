using AppComunicazioni.Interface;

namespace AppComunicazioni.Service
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTimeOffset.Now;
                    var nextRunTime = GetNextRunTime(now);
                    var delay = nextRunTime - now;

                    _logger.LogInformation($"Prossima esecuzione pianificata: {nextRunTime}. Attesa di {delay.TotalMinutes} minuti.");
                    await Task.Delay(delay, stoppingToken);

                    if (IsWeekday(nextRunTime))
                    {
                        await ExecuteTasks(nextRunTime, stoppingToken);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning("Servizio interrotto: {Message}", ex.Message);

                    // Creazione di uno scope per l'uso del servizio scoped
                    using var scope = _serviceProvider.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    try
                    {
                        await emailService.SendEmailAsync(
                            "simeone.eurocredit@gmail.com",
                            "Arresto del servizio rilevato",
                            $"L'applicazione è stata arrestata alle {DateTime.Now}. Errore: {ex.Message}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError("Errore durante l'invio della notifica email: {Message}", emailEx.Message);
                    }
                }
            }

            _logger.LogWarning("Servizio di background interrotto.");
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
                return nextRunMorning;
            if (now < nextRunEvening)
                return nextRunEvening;

            return nextRunMorning.AddDays(1);
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
