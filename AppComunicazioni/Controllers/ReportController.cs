using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;

[Route("Report")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("ManualReport")]
    public async Task<IActionResult> ManualReport()
    {
        try
        {
            _logger.LogInformation("Richiesta di invio manuale del report ricevuta.");
            await _reportService.GenerateAndSendDailyReportAsync();
            TempData["SuccessMessage"] = "Report inviato correttamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Errore durante l'invio del report: {ex.Message}");
            TempData["ErrorMessage"] = "Errore durante l'invio del report. Riprova più tardi.";
        }

        // Usa Redirect e passa direttamente l'URL del Referer
        var refererUrl = Request.Headers["Referer"].ToString();
        return Redirect(refererUrl);
    }
}
