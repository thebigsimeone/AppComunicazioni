using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
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

    public async Task CheckAndSendNotificationsAsync(List<int> destinatariIds = null)
    {
        try
        {
            // Usa DateTimeOffset.Now per la tua ora corrente con il fuso orario locale
            var currentTime = DateTimeOffset.Now;
            _logger.LogInformation($"Valore di currentTime: {currentTime}");

            // Recupera tutte le comunicazioni senza DateF e che non sono ancora state notificate
            _logger.LogInformation("Recupero tutte le comunicazioni senza DateF e non notificate...");
            var comunicazioniList = await _context.Comunicazionis
                .Where(x => !x.DateF.HasValue && (x.Notificato == false || x.Notificato == null))
                .ToListAsync();

            // Log del numero di comunicazioni recuperate
            _logger.LogInformation($"Trovate {comunicazioniList.Count} comunicazioni senza DateF e non notificate.");

            var notifications = new List<ComunicazioniWithDaysModel>();

            // Cicla attraverso tutte le comunicazioni
            foreach (var comunicazione in comunicazioniList)
            {
                var nomeServizio = comunicazione.Servizio?.ToUpper() ?? "";

                // Log per debug: valore di DateA letto dal database
                _logger.LogInformation($"Comunicazione ID: {comunicazione.Id}, Nome File: {comunicazione.FileName}, Servizio: {nomeServizio}, DateA (letto dal DB): {comunicazione.DateA}");

                // Calcola l'ora prevista di invio (basato su criteri del servizio)
                var invioPrevisto = comunicazione.DateA;
                switch (nomeServizio)
                {
                    case "035":
                        invioPrevisto = comunicazione.DateA?.AddDays(3);
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
                        invioPrevisto = comunicazione.DateA?.AddDays(5);
                        break;
                    case "ERE":
                        invioPrevisto = comunicazione.DateA?.AddDays(7);
                        break;
                    case "APP":
                        invioPrevisto = comunicazione.DateA?.AddDays(8);
                        break;
                    default:
                        invioPrevisto = null;
                        break;
                }

                _logger.LogInformation($"Giorno previsto per la comunicazione '{comunicazione.FileName}': {invioPrevisto}");

                // Verifica se è ora di inviare la notifica in base all'ora corrente e al tempo previsto
                if (invioPrevisto.HasValue && currentTime >= invioPrevisto)
                {
                    var totalMinutes = (currentTime - comunicazione.DateA)?.TotalMinutes ?? 0;
                    var totalDays = (currentTime - comunicazione.DateA)?.TotalDays ?? 0;

                    var comunicazioneModel = new ComunicazioniWithDaysModel
                    {
                        Comunicazioni = comunicazione,
                        NomeServizio = nomeServizio,
                        TotalMinutes = totalMinutes,
                        TotalDays = totalDays
                    };

                    // Verifica se la comunicazione soddisfa i criteri di notifica usando ControlloServizio
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

            // Log del numero di notifiche che devono essere inviate
            _logger.LogInformation($"Trovate {notifications.Count} comunicazioni che necessitano di notifica.");

            // Invia le notifiche
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

                    // Dopo aver inviato l'email, segna la comunicazione come notificata
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

    // Funzione per verificare se la comunicazione soddisfa i criteri di notifica
    private bool ControlloServizio(ComunicazioniWithDaysModel comunicazione)
    {
        var matches = false;

        // Applica i criteri di notifica in base al servizio
        switch (comunicazione.NomeServizio)
        {
            case "035":
                matches = comunicazione.TotalDays >= 3;
                break;
            case "PDL":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "DIM":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "MA7":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "ERE":
                matches = comunicazione.TotalDays >= 7;
                break;
            case "MIM":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "APP":
                matches = comunicazione.TotalDays >= 8;
                break;
            case "VSA":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "VL1":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "VLA":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "VL3":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "VSS":
                matches = comunicazione.TotalDays >= 5;
                break;
            case "VPP":
                matches = comunicazione.TotalDays >= 5;
                break;
            default:
                matches = false;
                break;
        }

        return matches;
    }

    public async Task StopMonitoringForComunicazioneAsync(int comunicazioneId)
    {
        try
        {
            var comunicazione = await _context.Comunicazionis.FindAsync(comunicazioneId);
            if (comunicazione != null)
            {
                // Assumiamo che l'interruzione del monitoraggio possa essere semplicemente l'assegnazione di una data di smarco (DateF)
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
