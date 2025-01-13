using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class SendMailService : ISendMailService
{
    private readonly ComDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IStopMonitoringService _stopMonitoringService; // Usa solo questo
    private readonly ILogger<SendMailService> _logger;

    public SendMailService(ComDbContext context, IEmailService emailService,
                           IStopMonitoringService stopMonitoringService, ILogger<SendMailService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _stopMonitoringService = stopMonitoringService ?? throw new ArgumentNullException(nameof(stopMonitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandlePostEditActionsAsync(Comunicazioni comunicazioniToUpdate)
    {
        await SendMarkingEmailsAsync(comunicazioniToUpdate);

        // Aggiorna Email_inviata
        comunicazioniToUpdate.Email_inviata = true;
        _context.Comunicazionis.Update(comunicazioniToUpdate);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Email inviata e Email_inviata impostato a true per il file: {comunicazioniToUpdate.FileName}");
    }

    public async Task SendMarkingEmailsAsync(Comunicazioni comunicazioni)
    {
        bool emailSuccess = true;
        var destinatari = await _context.Destinataris.Where(d => d.Attivo == "S").ToListAsync();

        if (!destinatari.Any())
        {
            _logger.LogWarning("Non ci sono destinatari attivi.");
            return;
        }

        string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioni.FileName}";
        string formattedNote = !string.IsNullOrEmpty(comunicazioni.Note)
            ? _emailService.FormatNote(comunicazioni.Note)
            : "Nessuna nota disponibile.";

        string message = $"<p>Il seguente file è stato smarcato: {comunicazioni.FileName}<br>" +
                         $"Numero protocolli: {comunicazioni.NProtocol}<br>" +
                         $"Protocolli da controllare: {comunicazioni.NsProtocol}<br>" +
                         $"{formattedNote}<br>" +
                         "Cordiali saluti,<br>Flavio Simeone</p>";

        foreach (var destinatario in destinatari)
        {
            if (!string.IsNullOrEmpty(destinatario.Destinatario))
            {
                try
                {
                    await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message);
                    _logger.LogInformation($"Email inviata a: {destinatario.Destinatario}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore invio email a {destinatario.Destinatario}: {ex.Message}");
                    emailSuccess = false;
                }
            }
            else
            {
                _logger.LogWarning("Il destinatario non è valido (null o vuoto).");
                emailSuccess = false;
            }
        }

        if (!emailSuccess)
        {
            _logger.LogWarning("Invio email fallito. Flag Email_inviata non aggiornato.");
        }
    }

    public async Task SendNotificationEmailAsync(List<ComunicazioniWithDaysModel> notifications)
    {
        foreach (var notification in notifications)
        {
            var comunicazione = notification.Comunicazioni;

            if (comunicazione == null)
            {
                _logger.LogWarning("Comunicazione null nel modello di notifica.");
                continue;
            }

            // Verifica se la comunicazione è già stata notificata
            if (comunicazione.Notificato == true)
            {
                _logger.LogInformation($"La comunicazione '{comunicazione.FileName}' è già stata notificata.");
                continue;
            }

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
                    .Where(d => d.Attivo == "S" && d.Monitor == "S")
                    .ToListAsync();

                if (destinatari == null || destinatari.Count == 0)
                {
                    _logger.LogInformation("Nessun destinatario trovato per inviare le notifiche.");
                    continue;
                }

                foreach (var destinatario in destinatari)
                {
                    if (!string.IsNullOrEmpty(destinatario?.Destinatario))
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
                    else
                    {
                        _logger.LogWarning("Destinatario con indirizzo email nullo o vuoto.");
                    }
                }

                comunicazione.Notificato = true;
                comunicazione.Data_Notifica = DateTimeOffset.Now;

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

    public async Task SendEmailReportAsync(List<Comunicazioni> ritardi, List<Comunicazioni> ritorni)
    {
        var today = DateTimeOffset.Now.ToString("dd/MM/yyyy");

        var destinatari = await _context.Destinataris
            .Where(d => d.Attivo == "S" && d.Report == "S")
            .ToListAsync();

        if (!destinatari.Any())
        {
            _logger.LogWarning("Nessun destinatario trovato per il report giornaliero.");
            return;
        }

        var subject = $"Report giornaliero file ritardi e ritorni - {today}";
        var body = new StringBuilder();
        body.AppendLine($"<h3>Report giornaliero {today}</h3>");

        if (ritardi.Any())
        {
            body.AppendLine("<h4>File in ritardo:</h4><ul>");
            foreach (var r in ritardi)
            {
                body.AppendLine($"<li>{r.FileName} - Notificato il {r.Data_Notifica:dd/MM/yyyy}</li>");
            }
            body.AppendLine("</ul>");
        }

        if (ritorni.Any())
        {
            body.AppendLine("<h4>File ritornati e smarcati:</h4><ul>");
            foreach (var r in ritorni)
            {
                body.AppendLine($"<li>{r.FileName} - Ritornato il {r.DateF:dd/MM/yyyy}</li>");
            }
            body.AppendLine("</ul>");
        }

        foreach (var destinatario in destinatari)
        {
            if (!string.IsNullOrEmpty(destinatario.Destinatario))
            {
                try
                {
                    await _emailService.SendEmailAsync(destinatario.Destinatario, subject, body.ToString());
                    _logger.LogInformation($"Email report inviata a: {destinatario.Destinatario}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante l'invio del report a {destinatario.Destinatario}: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("Destinatario con indirizzo email nullo o vuoto.");
            }
        }
    }
}