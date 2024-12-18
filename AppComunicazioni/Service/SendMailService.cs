using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

public class SendMailService : ISendMailService
{
    private readonly ComDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<SendMailService> _logger;

    public SendMailService(ComDbContext context, IEmailService emailService, IMonitoringService monitoringService, ILogger<SendMailService> logger)
    {
        _context = context;
        _emailService = emailService;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    public async Task HandlePostEditActionsAsync(Comunicazioni comunicazioniToUpdate)
    {
        if (comunicazioniToUpdate.DateF != null)
        {
            await SendMarkingEmailsAsync(comunicazioniToUpdate);
            await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
        }
    }

    public async Task SendMarkingEmailsAsync(Comunicazioni comunicazioni)
    {
        if (comunicazioni.Email_inviata == true)
        {
            _logger.LogInformation($"Email già inviata per il file: {comunicazioni.FileName}. Nessuna azione richiesta.");
            return;
        }

        bool emailSuccess = true;
        var destinatari = await _context.Destinataris.Where(d => d.Attivo == "S").ToListAsync();

        if (destinatari.Count == 0)
        {
            _logger.LogWarning("Non ci sono destinatari attivi.");
        }
        else
        {
            string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioni.FileName}";
            string formattedNote = !string.IsNullOrEmpty(comunicazioni.Note)
                ? _emailService.FormatNote(comunicazioni.Note)
                : "Nessuna nota disponibile.";

            string message = $"<p>Il seguente file è stato smarcato: {comunicazioni.FileName}<br>" +
                             $"con il numero protocolli: {comunicazioni.NProtocol}<br><br>" +
                             $"Questi sono i protocolli da controllare: {comunicazioni.NsProtocol}<br><br>" +
                             $"{formattedNote}<br><br>" +
                             $"Cordiali saluti,<br>Flavio Simeone</p>";

            foreach (var destinatario in destinatari)
            {
                if (!string.IsNullOrEmpty(destinatario.Destinatario))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore invio email a: {destinatario.Destinatario} | {ex.Message}");
                        emailSuccess = false;
                    }
                }
                else
                {
                    _logger.LogWarning("Il destinatario non è valido (null o vuoto). Email non inviata.");
                    emailSuccess = false;
                }
            }
        }

        // Aggiorna la colonna Email_inviata
        if (emailSuccess)
        {
            comunicazioni.Email_inviata = true;
            _context.Comunicazionis.Update(comunicazioni);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Email inviata con successo per il file: {comunicazioni.FileName}");
        }
        else
        {
            _logger.LogWarning("Invio email fallito. Email_inviata non aggiornata.");
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
                    .Where(d => d.Monitor == "S")
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
}