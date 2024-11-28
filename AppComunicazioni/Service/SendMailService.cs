using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            await SendNotificationEmailsAsync(comunicazioniToUpdate);
            await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
        }
    }

    public async Task SendNotificationEmailsAsync(Comunicazioni comunicazioni)
    {
        bool emailSuccess = true;
        var destinatari = await _context.Destinataris.Where(d => d.Attivo == "S").ToListAsync();

        if (destinatari.Count == 0)
        {
            _logger.LogWarning("Non ci sono destinatari attivi.");
        }
        else
        {
            string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioni.FileName}";
            string formattedNote = _emailService.FormatNote(comunicazioni.Note);
            string message = $"<p>Il seguente file è stato smarcato: {comunicazioni.FileName}<br>" +
                             $"con il numero protocolli: {comunicazioni.NProtocol}<br><br>" +
                             $"Questi sono i protocolli da controllare: {comunicazioni.NsProtocol}<br><br>" +
                             $"{formattedNote}<br><br>" +
                             $"Cordiali saluti,<br>Flavio Simeone</p>";

            foreach (var destinatario in destinatari)
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
        }

        // Puoi utilizzare TempData in un controller, ma non in un servizio, quindi potrebbe essere necessario un modo alternativo per fornire feedback all'utente
        if (emailSuccess)
        {
            _logger.LogInformation("Comunicazione modificata e email inviate con successo.");
        }
        else
        {
            _logger.LogWarning("Comunicazione modificata, ma l'invio delle email è fallito.");
        }
    }
}
