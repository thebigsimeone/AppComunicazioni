using AppComunicazioni.Data;
using AppComunicazioni.Interface;

public class StopMonitoringService : IStopMonitoringService
{
    private readonly ComDbContext _context;
    private readonly ILogger<StopMonitoringService> _logger;

    public StopMonitoringService(ComDbContext context, ILogger<StopMonitoringService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StopMonitoringForComunicazioneAsync(int comunicazioneId)
    {
        try
        {
            var comunicazione = await _context.Comunicazionis.FindAsync(comunicazioneId);
            if (comunicazione != null)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Monitoraggio per ID {comunicazioneId} interrotto.");
            }
            else
            {
                _logger.LogWarning($"Nessuna comunicazione trovata con ID {comunicazioneId}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Errore durante l'interruzione del monitoraggio per ID {comunicazioneId}: {ex.Message}");
        }
    }
}
