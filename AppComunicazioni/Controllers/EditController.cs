using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    [Route("[controller]")]
    public class EditController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRetryService _retryService;
        private readonly IViewBagService _viewBagService;
        private readonly ISendMailService _sendMailService;
        private readonly ILogger<EditController> _logger;
        private readonly IEncryptionService _encryptionService;

        public EditController(ComDbContext context, IMapper mapper,
                              IRetryService retryService, IViewBagService viewBagService,
                              ISendMailService sendMailService, IEncryptionService encryptionService,
                              ILogger<EditController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("EditController istanziato correttamente.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            int decryptedId = DecryptId(id);
            if (decryptedId == -1)
                return BadRequest("ID non valido.");

            var comunicazioni = await _context.Comunicazionis
                                .AsNoTracking() // Garantisce che il valore venga caricato dal database
                                .FirstOrDefaultAsync(c => c.Id == decryptedId);
            if (comunicazioni == null)
                return NotFound();

            _logger.LogInformation($"Email_inviata al caricamento: {comunicazioni.Email_inviata}");

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

            ViewData["EncryptedId"] = id ?? string.Empty; // Assegna un valore predefinito per evitare null
            SetViewBagOptions();
            return View(comunicazioniDTO);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string id, [Bind] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("ID nullo o vuoto passato al metodo Index.");
                return BadRequest("ID non valido.");
            }

            int decryptedId = DecryptId(id);
            if (decryptedId == -1)
            {
                _logger.LogError("ID decriptato non valido.");
                return BadRequest("ID non valido.");
            }

            comunicazioniDTO.Id = decryptedId;
            ModelState.Clear();
            TryValidateModel(comunicazioniDTO);

            if (!ModelState.IsValid)
            {
                LogModelStateErrors();
                ViewData["EncryptedId"] = id ?? string.Empty;
                SetViewBagOptions();
                return View(comunicazioniDTO);
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var comunicazioniToUpdate = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == decryptedId);

                if (comunicazioniToUpdate == null)
                {
                    _logger.LogWarning($"Comunicazione con ID {decryptedId} non trovata.");
                    return NotFound();
                }

                _logger.LogInformation($"Prima del reload - DateF: {comunicazioniToUpdate.DateF}, Email_inviata: {comunicazioniToUpdate.Email_inviata}");
                await _context.Entry(comunicazioniToUpdate).ReloadAsync();

                // Aggiorna i valori
                comunicazioniToUpdate.DateF = comunicazioniDTO.DateF;
                comunicazioniToUpdate.Note = comunicazioniDTO.Note;
                _logger.LogInformation($"Valori aggiornati - DateF: {comunicazioniToUpdate.DateF}, Email_inviata: {comunicazioniToUpdate.Email_inviata}");

                // Logica per invio email
                if (comunicazioniToUpdate.Email_inviata.HasValue && !comunicazioniToUpdate.Email_inviata.Value)
                {
                    _logger.LogInformation($"Invio email per il file: {comunicazioniToUpdate.FileName}");
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                    comunicazioniToUpdate.Email_inviata = true;
                }

                // Salva le modifiche
                _logger.LogInformation($"Prima del salvataggio - DateF: {comunicazioniToUpdate.DateF}, Email_inviata: {comunicazioniToUpdate.Email_inviata}");
                _context.Entry(comunicazioniToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Modifiche salvate correttamente per il file: {comunicazioniToUpdate.FileName}");

                // Determina il controller di destinazione
                var referer = Request.Headers["Referer"].ToString();
                string controllerName = comunicazioniToUpdate.FileName?.Contains("SSC") == true ? "ComunicazionisSsc" : "ComunicazionisEbi";

                if (!string.IsNullOrEmpty(referer))
                {
                    return RedirectToAction("Index", controllerName);
                }

                return RedirectToAction("Index", controllerName);
            }, _logger, this);
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                return int.Parse(_encryptionService.Decrypt(encryptedId) ?? "-1"); // Gestisce null
            }
            catch
            {
                return -1; // Ritorna -1 in caso di errore
            }
        }

        private void LogModelStateErrors()
        {
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors == null) continue;

                foreach (var error in errors)
                {
                    Console.WriteLine($"Chiave: {key}, Errore: {error.ErrorMessage}");
                }
            }
        }

        private void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor) ?? new List<SelectListItem>();
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor) ?? new List<SelectListItem>();
        }
    }
}
