using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using Microsoft.EntityFrameworkCore;

public class ReportService : IReportService
{
    private readonly ComDbContext _context;
    private readonly ISendMailService _sendMailService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ComDbContext context, ISendMailService sendMailService, ILogger<ReportService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task GenerateAndSendDailyReportAsync()
    {
        var today = DateTimeOffset.Now.Date;

        var ritardi = await _context.Comunicazionis
            .Where(c => c.Notificato == true && c.Data_Notifica.HasValue && c.Data_Notifica.Value.Date == today && c.Report == "R")
            .ToListAsync();

        var ritorni = await _context.Comunicazionis
            .Where(c => c.Ritornato == true && c.DateF.HasValue && c.DateF.Value.Date == today)
            .ToListAsync();


        if (!ritardi.Any() && !ritorni.Any())
        {
            _logger.LogInformation("Nessun ritardo o ritorno da notificare per oggi.");
            return;
        }

        await _sendMailService.SendEmailReportAsync(ritardi, ritorni);
        _logger.LogInformation("Report giornaliero inviato con successo.");
    }
}
