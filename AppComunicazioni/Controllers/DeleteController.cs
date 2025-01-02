using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    [Route("[controller]")]
    public class DeleteController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IRetryService _retryService;
        private readonly ILogger<DeleteController> _logger;
        private readonly IEncryptionService _encryptionService;

        public DeleteController(
            ComDbContext context,
            IRetryService retryService,
            ILogger<DeleteController> logger,
            IEncryptionService encryptionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("ID crittografato vuoto o nullo.");
                return NotFound();
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var decryptedId = DecryptId(id);
                if (decryptedId == -1)
                {
                    _logger.LogError("Errore nella decrittazione dell'ID: {EncryptedId}", id);
                    return BadRequest("ID non valido.");
                }

                var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == decryptedId);
                if (comunicazioni == null)
                {
                    _logger.LogWarning("Comunicazione non trovata per ID: {DecryptedId}", decryptedId);
                    return NotFound();
                }

                return View(comunicazioni);
            }, _logger, this);
        }

        [HttpPost("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                var comunicazioni = await _context.Comunicazionis
                    .Include(c => c.Dettagli)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comunicazioni == null)
                {
                    _logger.LogWarning("Comunicazione non trovata durante l'eliminazione. ID: {Id}", id);
                    return NotFound();
                }

                // Elimina i dettagli associati, se presenti
                if (comunicazioni.Dettagli != null && comunicazioni.Dettagli.Any())
                {
                    _context.ComunicazioniDettagli.RemoveRange(comunicazioni.Dettagli);
                }

                _context.Comunicazionis.Remove(comunicazioni);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comunicazione eliminata correttamente. ID: {Id}", id);
                return RedirectToAction("Index", "Home"); // Modifica con il controller di destinazione.
            }, _logger, this);
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                var decryptedText = _encryptionService.Decrypt(encryptedId);
                return int.TryParse(decryptedText, out var id) ? id : -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la decrittazione dell'ID crittografato: {EncryptedId}", encryptedId);
                return -1;
            }
        }
    }
}
