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
                         "Cordiali saluti,<br>Team Comunicazioni</p>";

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
        else
        {
            await _stopMonitoringService.StopMonitoringForComunicazioneAsync(comunicazioni.Id);
            _logger.LogInformation($"Monitoraggio per la comunicazione '{comunicazioni.FileName}' interrotto.");
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
            message.AppendLine("Cordiali saluti,<br><br>Team Comunicazioni</p>");

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

                await _stopMonitoringService.StopMonitoringForComunicazioneAsync(comunicazione.Id);
                _logger.LogInformation($"Monitoraggio per la comunicazione '{comunicazione.FileName}' interrotto.");
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

        var subject = $"📊 Report Giornaliero - Ritardi e Ritorni | {today}";
        var body = new StringBuilder();

        // 🔵 HEADER DEL REPORT
        body.AppendLine($@"
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f9f9f9;
            color: #333;
            padding: 20px;
        }}
        h2 {{
            color: #004085;
            background-color: #CCE5FF;
            padding: 10px;
            border-radius: 5px;
            text-align: center;
        }}
        .section {{
            background-color: #ffffff;
            border: 1px solid #ddd;
            padding: 15px;
            margin-top: 20px;
            border-radius: 5px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        th, td {{
            padding: 10px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }}
        th {{
            background-color: #007BFF;
            color: white;
        }}
        tr:nth-child(even) {{
            background-color: #f2f2f2;
        }}
        tr:hover {{
            background-color: #e9ecef;
        }}
        .fornitore-header {{
            background-color: #343a40;
            color: white;
            padding: 8px;
            border-radius: 5px;
            margin-top: 10px;
            text-transform: uppercase;
        }}
    </style>
</head>
<body>
    <h2>📅 Report Giornaliero - {today}</h2>
");

        // 🔴 FILE IN RITARDO
        if (ritardi.Any())
        {
            body.AppendLine("<div class='section'>");
            body.AppendLine("<h3 style='color: #dc3545;'>🚨 File in Ritardo</h3>");
            body.AppendLine("<table>");
            body.AppendLine("<tr><th>Nome File</th><th>Data Notifica</th></tr>");
            foreach (var r in ritardi)
            {
                body.AppendLine($"<tr><td>{r.FileName}</td><td>{r.Data_Notifica:dd/MM/yyyy}</td></tr>");
            }
            body.AppendLine("</table>");
            body.AppendLine("</div>");
        }

        // 🟢 FILE RITORNATI E SMARCATI PER FORNITORE
        if (ritorni.Any())
        {
            body.AppendLine("<div class='section'>");
            body.AppendLine("<h3 style='color: #28a745;'>📦 File Ritornati e Smarcati per Fornitore</h3>");

            var ritorniPerFornitore = ritorni
                .GroupBy(r => r.Fornitore)
                .OrderBy(g => g.Key);

            foreach (var gruppo in ritorniPerFornitore)
            {
                string nomeFornitore = gruppo.Key.ToString();

                body.AppendLine($"<div class='fornitore-header'>🔹 {nomeFornitore}</div>");
                body.AppendLine("<table>");
                body.AppendLine("<tr><th>Nome File</th><th>Data Ritorno</th></tr>");
                foreach (var r in gruppo)
                {
                    body.AppendLine($"<tr><td>{r.FileName}</td><td>{r.DateF:dd/MM/yyyy}</td></tr>");
                }
                body.AppendLine("</table>");
            }

            body.AppendLine("</div>");
        }

        // 🔵 FOOTER
        body.AppendLine(@"
    <br><br>
    <p style='font-size: 12px; color: #6c757d;'>🔒 Questo è un messaggio automatico. Si prega di non rispondere a questa email.</p>
    <p style='font-weight: bold;'>Cordiali saluti,<br>Team Comunicazioni</p>
</body>
</html>
");

        // 📧 INVIO EMAIL
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