using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class MonitoringService : IMonitoringService
{
    private readonly ComDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(ComDbContext context, IEmailService emailService, ILogger<MonitoringService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CheckAndSendNotificationsAsync(List<int>? destinatariIds = null)
    {
        try
        {
            var currentTime = DateTimeOffset.Now;
            _logger.LogInformation($"Valore di currentTime: {currentTime}");

            _logger.LogInformation("Recupero tutte le comunicazioni senza DateF e non notificate...");
            var comunicazioniList = await _context.Comunicazionis
                .Where(x => !x.DateF.HasValue && (x.Notificato == false || x.Notificato == null))
                .ToListAsync();

            _logger.LogInformation($"Trovate {comunicazioniList.Count} comunicazioni senza DateF e non notificate.");

            var notifications = new List<ComunicazioniWithDaysModel>();

            foreach (var comunicazione in comunicazioniList)
            {
                var nomeServizio = comunicazione.Servizio?.ToUpper() ?? "";

                _logger.LogInformation($"Comunicazione ID: {comunicazione.Id}, Nome File: {comunicazione.FileName}, Servizio: {nomeServizio}, DateA (letto dal DB): {comunicazione.DateA}");

                int giorniDaAggiungere;
                switch (nomeServizio)
                {
                    case "S035":
                        giorniDaAggiungere = 3;
                        break;
                    case "PDL":
                    case "DIM":
                    case "MA7":
                    case "MIM":
                    case "VL1":
                    case "VLA":
                    case "VL3":
                    case "VSA":
                    case "VPP":
                    case "VSS":
                        giorniDaAggiungere = 5;
                        break;
                    case "ERE":
                        giorniDaAggiungere = 7;
                        break;
                    case "APP":
                        giorniDaAggiungere = 8;
                        break;
                    default:
                        giorniDaAggiungere = 0;
                        break;
                }

                var invioPrevisto = comunicazione.DateA.HasValue
                    ? CalcolaDataInvio(comunicazione.DateA.Value, giorniDaAggiungere)
                    : (DateTimeOffset?)null;

                _logger.LogInformation($"Giorno previsto per la comunicazione '{comunicazione.FileName}': {invioPrevisto}");

                if (invioPrevisto.HasValue && currentTime >= invioPrevisto)
                {
                    var totalDays = CalcolaGiorniLavorativi(comunicazione.DateA.Value, (currentTime - comunicazione.DateA.Value).Days);

                    var comunicazioneModel = new ComunicazioniWithDaysModel
                    {
                        Comunicazioni = comunicazione,
                        NomeServizio = nomeServizio,
                        TotalMinutes = (currentTime - comunicazione.DateA)?.TotalMinutes ?? 0,
                        TotalDays = totalDays
                    };

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

            foreach (var notification in notifications)
            {
                var comunicazione = notification.Comunicazioni;

                var subject = $"Notifica ritardo: {comunicazione.FileName}";
                var message = new StringBuilder();
                message.AppendLine("<p>Attenzione, il seguente file necessita di verifica:<br>");
                message.AppendLine($"Nome del file: {comunicazione.FileName}<br>");
                message.AppendLine($"Data di invio: {comunicazione.DateA:dd/MM/yyyy HH:mm:ss}<br>");
                message.AppendLine("<br>Il file non è stato ancora smarcato e il limite di tempo previsto è stato superato.<br>");
                message.AppendLine("Cordiali saluti,<br><br>App Comunicazioni</p>");

                try
                {
                    var destinatari = await _context.Destinataris
                        .Where(d => d.Monitor == "S")
                        .ToListAsync();

                    if (destinatari == null || destinatari.Count == 0)
                    {
                        _logger.LogInformation("Nessun destinatario trovato per inviare le notifiche.");
                        continue;
                    }

                    foreach (var destinatario in destinatari)
                    {
                        try
                        {
                            _logger.LogInformation($"Invio email a: {destinatario.Destinatario}");
                            await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message.ToString());
                            _logger.LogInformation($"Email inviata a: {destinatario.Destinatario}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Errore durante l'invio dell'email a {destinatario.Destinatario}: {ex.Message}");
                        }
                    }

                    comunicazione.Notificato = true;
                    _context.Comunicazionis.Update(comunicazione);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Comunicazione '{comunicazione.FileName}' è stata notificata.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante il recupero dei destinatari: {ex.Message}");
                }
            }
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
            "APP" => comunicazione.TotalDays >= 8,
            _ => false,
        };
    }

    public async Task StopMonitoringForComunicazioneAsync(int comunicazioneId)
    {
        try
        {
            var comunicazione = await _context.Comunicazionis.FindAsync(comunicazioneId);
            if (comunicazione != null)
            {
                comunicazione.DateF = DateTime.Now;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Il monitoraggio per la comunicazione con ID {comunicazioneId} è stato interrotto.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Errore durante l'interruzione del monitoraggio per la comunicazione con ID {comunicazioneId}: {ex.Message}");
        }
    }
}
