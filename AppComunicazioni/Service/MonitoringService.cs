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
        var currentDate = DateTimeOffset.UtcNow;
        var notifications = new List<ComunicazioniWithDaysModel>();

        try
        {
            // Fetch all the rows that have no DateF
            Console.WriteLine("Recupero tutte le comunicazioni senza DateF...");
            var comunicazioniList = await _context.Comunicazionis
                .Where(x => !x.DateF.HasValue)
                .ToListAsync();

            // Log the number of records fetched
            Console.WriteLine($"Trovate {comunicazioniList.Count} comunicazioni senza DateF.");

            // Create a list with calculated TotalDays
            notifications = comunicazioniList
                .Select(x =>
                {
                    // Utilizza la colonna Servizio invece di FileName per determinare il servizio
                    var nomeServizio = x.Servizio?.ToUpper() ?? "";

                    // Log per debug: valore di DateA letto dal database
                    Console.WriteLine($"Comunicazione ID: {x.Id}, Nome File: {x.FileName}, Servizio: {nomeServizio}, DateA (letto dal DB): {x.DateA}");

                    return new ComunicazioniWithDaysModel
                    {
                        Comunicazioni = x,
                        NomeServizio = nomeServizio,
                        TotalDays = (currentDate - x.DateA)?.TotalDays ?? 0,
                        TotalMinutes = (currentDate - x.DateA)?.TotalMinutes ?? 0,
                        TotalHours = (currentDate - x.DateA)?.TotalHours ?? 0
                    };
                })
                .Where(x =>
                {
                    var matches = false;

                    // Determine the matching condition based on the service name
                    switch (x.NomeServizio)
                    {
                        case "035":
                            matches = x.TotalDays >= 3;
                            break;
                        case "PDL":
                            matches = x.TotalMinutes >= 5; // Deve aspettare 5 minuti
                            break;
                        case "DIM":
                            matches = x.TotalDays >= 5;
                            break;
                        case "MA7":
                            matches = x.TotalDays >= 5;
                            break;
                        case "ERE":
                            matches = x.TotalDays >= 7;
                            break;
                        case "MIM":
                            matches = x.TotalDays >= 5;
                            break;
                        case "APP":
                            matches = x.TotalDays >= 8;
                            break;
                        case "VSA":
                            matches = x.TotalDays >= 5;
                            break;
                        case "VPP":
                            matches = x.TotalDays >= 5;
                            break;
                        default:
                            matches = false;
                            break;
                    }

                    if (matches)
                    {
                        Console.WriteLine($"Comunicazione '{x.Comunicazioni.FileName}' soddisfa i criteri di notifica.");
                    }
                    else
                    {
                        Console.WriteLine($"Comunicazione '{x.Comunicazioni.FileName}' NON soddisfa i criteri di notifica.");
                    }

                    return matches;
                })
                .ToList();

            // Log the number of notifications that need to be sent
            Console.WriteLine($"Trovate {notifications.Count} comunicazioni che necessitano di notifica.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante il recupero delle comunicazioni: {ex.Message}");
            return;
        }

        // Send email notifications for each row that matches the criteria
        foreach (var notification in notifications)
        {
            var comunicazione = notification.Comunicazioni;

            // Log il valore di currentDate e DateA per capire la differenza di tempo
            Console.WriteLine($"Valore di currentDate: {currentDate}");
            Console.WriteLine($"Valore di DateA per la comunicazione '{comunicazione.FileName}': {comunicazione.DateA}");

            var differenceInMinutes = (currentDate - comunicazione.DateA)?.TotalMinutes ?? 0;
            Console.WriteLine($"Differenza in minuti tra currentDate e DateA per la comunicazione '{comunicazione.FileName}': {differenceInMinutes}");

            if (differenceInMinutes < 5)
            {
                Console.WriteLine($"L'email per la comunicazione '{comunicazione.FileName}' non viene ancora inviata perché non sono passati 5 minuti.");
                continue;
            }

            var subject = $"Notifica ritardo: {comunicazione.FileName}";
            var message = new StringBuilder();
            message.AppendLine("<p>Attenzione, il seguente file necessita di verifica:<br>");
            message.AppendLine($"Nome del file: {comunicazione.FileName}<br>");
            message.AppendLine($"Data di invio: {comunicazione.DateA:dd/MM/yyyy}<br>");
            message.AppendLine("<br>Il file non è stato ancora smarcato e il limite di tempo previsto è stato superato.<br>");
            message.AppendLine("Cordiali saluti,<br><br>App Comunicazioni</p>");

            try
            {
                var destinatari = await _context.Destinataris
                                .Where(d => d.Monitor == "S")
                                .ToListAsync();

                if (destinatari == null || destinatari.Count == 0)
                {
                    Console.WriteLine("Nessun destinatario trovato per inviare le notifiche.");
                    continue;
                }

                foreach (var destinatario in destinatari)
                {
                    try
                    {
                        Console.WriteLine($"Invio email a: {destinatario.Destinatario}");
                        await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message.ToString());
                        Console.WriteLine($"Email inviata a: {destinatario.Destinatario}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore durante l'invio dell'email a {destinatario.Destinatario}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei destinatari: {ex.Message}");
            }
        }
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
                Console.WriteLine($"Il monitoraggio per la comunicazione con ID {comunicazioneId} è stato interrotto.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'interruzione del monitoraggio per la comunicazione con ID {comunicazioneId}: {ex.Message}");
        }
    }
}
