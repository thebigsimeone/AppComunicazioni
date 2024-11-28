using AppComunicazioni.Data;
using Microsoft.EntityFrameworkCore;

public class CleanupService
{
    private readonly ComDbContext _context;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(ComDbContext context, ILogger<CleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanupOldRecordsAsync()
    {
        var cutoffDate = DateTime.Now.AddMonths(-2);
        _logger.LogInformation($"Inizio pulizia dei record con data di inserimento precedente a {cutoffDate}");

        var oldRecords = await _context.ComunicazioniDettagli
            .Where(d => d.DataInserimento < cutoffDate)
            .ToListAsync();

        if (oldRecords.Any())
        {
            _context.ComunicazioniDettagli.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"{oldRecords.Count} record eliminati con successo.");
        }
        else
        {
            _logger.LogInformation("Nessun record da eliminare trovato.");
        }
    }
}
