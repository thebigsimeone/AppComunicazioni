using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    [Route("[controller]")]
    public class DetailsController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRetryService _retryService;
        private readonly ILogger<DetailsController> _logger;
        private readonly IEncryptionService _encryptionService;

        public DetailsController(
            ComDbContext context,
            IMapper mapper,
            IRetryService retryService,
            ILogger<DetailsController> logger,
            IEncryptionService encryptionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            _logger.LogInformation("Ricevuto ID crittografato: {Id}", id);

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("ID crittografato vuoto o nullo.");
                return NotFound();
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                int decryptedId = DecryptId(id);
                if (decryptedId == -1)
                {
                    _logger.LogError("Errore nella decrittazione dell'ID: {EncryptedId}", id);
                    return BadRequest("ID non valido.");
                }

                _logger.LogInformation("ID decrittato: {DecryptedId}", decryptedId);

                var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == decryptedId);
                if (comunicazioni == null)
                {
                    _logger.LogWarning("Comunicazione non trovata per ID: {DecryptedId}", decryptedId);
                    return NotFound();
                }

                // Caricamento dei dettagli
                await _context.Entry(comunicazioni).Collection(c => c.Dettagli!).LoadAsync();
                _logger.LogInformation("Dettagli caricati per ID: {DecryptedId}", decryptedId);

                var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

                ViewData["EncryptedId"] = id ?? string.Empty;
                return View("Index", comunicazioniDTO);
            }, _logger, this);
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                _logger.LogInformation("Tentativo di decrittazione dell'ID crittografato: {EncryptedId}", encryptedId);
                var decryptedText = _encryptionService.Decrypt(encryptedId);
                _logger.LogInformation("Decrittazione riuscita: {DecryptedText}", decryptedText);

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
