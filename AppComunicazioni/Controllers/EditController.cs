using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Service;
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
        private readonly IExcelService _excelService;

        public EditController(ComDbContext context, IMapper mapper,
                              IRetryService retryService, IViewBagService viewBagService,
                              ISendMailService sendMailService, IEncryptionService encryptionService,
                              ILogger<EditController> logger, IExcelService excelService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

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
                TempData["Error"] = "ID non valido.";
                return BadRequest("ID non valido.");
            }

            int decryptedId = DecryptId(id);
            if (decryptedId == -1)
            {
                TempData["Error"] = "Errore nella decrittazione dell'ID.";
                return BadRequest("ID non valido.");
            }

            comunicazioniDTO.Id = decryptedId;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Alcuni campi non sono validi. Controlla i dati inseriti.";
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
                    TempData["ToastMessage"] = "Errore durante il salvataggio.";
                    TempData["ToastType"] = "error";
                    return NotFound();
                }

                comunicazioniToUpdate.DateF = comunicazioniDTO.DateF;
                comunicazioniToUpdate.Note = comunicazioniDTO.Note;

                if (comunicazioniToUpdate.Email_inviata.HasValue && !comunicazioniToUpdate.Email_inviata.Value)
                {
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                    comunicazioniToUpdate.Email_inviata = true;
                }

/*                // Elaborazione del file Excel
                if (excelFile != null && excelFile.Length > 0)
                {
                   var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioniDTO.Id);
                   if (dettagli != null)
                   {
                        _context.ComunicazioniDettagli.AddRange(dettagli);
                        await _context.SaveChangesAsync();
                   }
                }*/

                _context.Entry(comunicazioniToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Operazione completata con successo.";
                TempData["ToastType"] = "success";

                string controllerName = comunicazioniToUpdate.FileName?.Contains("SSC") == true ? "ComunicazionisSsc" : "ComunicazionisEbi";
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
