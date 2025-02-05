using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

public class MonitoringService : IMonitoringService
{
    private readonly ComDbContext _context;
    private readonly ISendMailService _sendMailService; // Mantieni questa dipendenza
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(ComDbContext context, ISendMailService sendMailService, ILogger<MonitoringService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CheckAndSendNotificationsAsync(List<int>? destinatariIds = null)
    {
        try
        {
            var currentTime = DateTimeOffset.Now;
            _logger.LogInformation($"Valore di currentTime: {currentTime}");

            _logger.LogInformation("Inizio monitoraggio delle comunicazioni...");
            var stopwatch = Stopwatch.StartNew();

            var batchSize = 1000; // Dimensione del batch
            var comunicazioniList = await _context.Comunicazionis
                .Where(x => !x.DateF.HasValue && (x.Notificato == false || x.Notificato == null))
                .Take(batchSize)
                .ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation($"Recuperati {comunicazioniList.Count} record in {stopwatch.ElapsedMilliseconds} ms.");

            if (!comunicazioniList.Any())
            {
                _logger.LogWarning("Nessuna comunicazione trovata per la notifica.");
                return;
            }

            _logger.LogInformation($"Trovate {comunicazioniList.Count} comunicazioni senza DateF e non notificate.");

            var notifications = new List<ComunicazioniWithDaysModel>();

            foreach (var comunicazione in comunicazioniList)
            {
                if (comunicazione == null)
                {
                    _logger.LogWarning("Comunicazione null trovata nella lista.");
                    continue;
                }

                var nomeServizio = comunicazione.Servizio?.ToUpper() ?? "";
                _logger.LogInformation($"Elaborazione comunicazione ID: {comunicazione.Id}, Nome File: {comunicazione.FileName}, Servizio: {nomeServizio}");

                if (!comunicazione.DateA.HasValue)
                {
                    _logger.LogWarning($"Comunicazione '{comunicazione.FileName}' non ha una data di invio valida.");
                    continue;
                }

                int giorniDaAggiungere = nomeServizio switch
                {
                    "S035" => 3,
                    "PDL" or "DIM" or "MA7" or "MIM" or "VL1" or "VLA" or "VL3" or "VSA" or "VPP" or "VSS" => 5,
                    "ERE" => 7,
                    "APP" or "APT" or "APL" or "APT" => 9,
                    _ => 0,
                };

                var invioPrevisto = CalcolaDataInvio(comunicazione.DateA.Value, giorniDaAggiungere);

                _logger.LogInformation($"Invio previsto per la comunicazione '{comunicazione.FileName}': {invioPrevisto}");

                if (currentTime >= invioPrevisto)
                {
                    var totalDays = CalcolaGiorniLavorativi(comunicazione.DateA.Value, (currentTime - comunicazione.DateA.Value).Days);

                    var comunicazioneModel = new ComunicazioniWithDaysModel
                    {
                        Comunicazioni = comunicazione,
                        NomeServizio = nomeServizio,
                        TotalMinutes = (currentTime - comunicazione.DateA)?.TotalMinutes ?? 0,
                        TotalDays = totalDays
                    };

                    // Verifica se la comunicazione soddisfa i criteri di notifica
                    if (ControlloServizio(comunicazioneModel))
                    {
                        _logger.LogInformation($"Comunicazione '{comunicazione.FileName}' soddisfa i criteri di notifica.");
                        notifications.Add(comunicazioneModel);
                    }
                    else
                    {
                        _logger.LogInformation($"Comunicazione '{comunicazione.FileName}' NON soddisfa i criteri di notifica.");
                    }
                }
                else
                {
                    _logger.LogInformation($"Comunicazione '{comunicazione.FileName}' NON soddisfa i criteri di invio temporale.");
                }
            }

            _logger.LogInformation($"Trovate {notifications.Count} comunicazioni che necessitano di notifica.");
            await _sendMailService.SendNotificationEmailAsync(notifications);
            _logger.LogInformation("Notifiche inviate con successo.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Errore durante il recupero delle comunicazioni: {ex.Message}");
        }
    }

    // Funzione per calcolare i giorni lavorativi e ottenere la data di invio
    private DateTimeOffset CalcolaDataInvio(DateTimeOffset dataInizio, int giorniDaAggiungere)
    {
        DateTimeOffset dataCorrente = dataInizio;

        while (giorniDaAggiungere > 0)
        {
            dataCorrente = dataCorrente.AddDays(1);
            if (dataCorrente.DayOfWeek != DayOfWeek.Saturday && dataCorrente.DayOfWeek != DayOfWeek.Sunday)
            {
                giorniDaAggiungere--;
            }
        }

        return dataCorrente;
    }

    // Funzione per calcolare i giorni lavorativi tra due date
    private int CalcolaGiorniLavorativi(DateTimeOffset dataInizio, int giorniTotali)
    {
        int giorniLavorativi = 0;
        DateTimeOffset dataCorrente = dataInizio;

        for (int i = 0; i < giorniTotali; i++)
        {
            dataCorrente = dataCorrente.AddDays(1);
            if (dataCorrente.DayOfWeek != DayOfWeek.Saturday && dataCorrente.DayOfWeek != DayOfWeek.Sunday)
            {
                giorniLavorativi++;
            }
        }

        return giorniLavorativi;
    }

    // Funzione per verificare se la comunicazione soddisfa i criteri di notifica
    private bool ControlloServizio(ComunicazioniWithDaysModel comunicazione)
    {
        return comunicazione.NomeServizio switch
        {
            "S035" => comunicazione.TotalDays >= 3,
            "PDL" or "DIM" or "MA7" or "MIM" or "VL1" or "VLA" or "VL3" or "VSA" or "VPP" or "VSS" => comunicazione.TotalDays >= 5,
            "ERE" => comunicazione.TotalDays >= 7,
            "APP" or "APT" or "APL" or "ATS" => comunicazione.TotalDays >= 9,
            _ => false,
        };
    }

}
