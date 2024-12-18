using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Controllers
{
    [Route("[controller]")]
    public class EditController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRetryService _retryService;
        private readonly IViewBagService _viewBagService;
        private readonly IMonitoringService _monitoringService;
        private readonly ISendMailService _sendMailService;
        private readonly IExcelService _excelService;
        private readonly IEncryptionService _encryptionService;

        public EditController(ComDbContext context, IMapper mapper, IRetryService retryService,
                              IViewBagService viewBagService, IMonitoringService monitoringService,
                              ISendMailService sendMailService, IExcelService excelService,
                              IEncryptionService encryptionService)
        {
            _context = context;
            _mapper = mapper;
            _retryService = retryService;
            _viewBagService = viewBagService;
            _monitoringService = monitoringService;
            _sendMailService = sendMailService;
            _excelService = excelService;
            _encryptionService = encryptionService;
        }

        // GET: Edit/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            int decryptedId = DecryptId(id);
            if (decryptedId == -1) return BadRequest("ID non valido.");

            var comunicazioni = await _context.Comunicazionis.FindAsync(decryptedId);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

            ViewData["EncryptedId"] = id;
            SetViewBagOptions();
            return View(comunicazioniDTO);
        }

        // POST: Edit/{id}
        [HttpPost("{id}")]
        public async Task<IActionResult> Index(string id, [Bind] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
        {
            int decryptedId = DecryptId(id); // Decripta l'ID dalla route
            if (decryptedId == -1) return BadRequest("ID non valido.");

            // Assicurati che l'ID decriptato corrisponda al modello
            comunicazioniDTO.Id = decryptedId;

            ModelState.Clear(); // Puliamo il ModelState per rimuovere problemi di binding sull'ID
            TryValidateModel(comunicazioniDTO); // Ricalcoliamo la validazione del modello

            if (!ModelState.IsValid)
            {
                LogModelStateErrors();
                ViewData["EncryptedId"] = id; // Reimposta l'ID crittografato
                SetViewBagOptions();
                return View(comunicazioniDTO);
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(decryptedId);
                if (comunicazioniToUpdate == null) return NotFound();

                _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

                // Logica DateF e Ritornato
                if (comunicazioniDTO.DateF.HasValue)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    comunicazioniToUpdate.Email_inviata = false; // Assicura che l'email venga inviata
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                    await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                }
                else if (comunicazioniDTO.Ritornato)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    comunicazioniToUpdate.DateF = null;
                    comunicazioniToUpdate.Email_inviata = true; // Nessuna email inviata
                    await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                }

                await _context.SaveChangesAsync();

                // Gestione file Excel
                if (excelFile != null && excelFile.Length > 0)
                {
                    var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioniToUpdate.Id);
                    if (dettagli != null)
                    {
                        _context.ComunicazioniDettagli.AddRange(dettagli);
                        await _context.SaveChangesAsync();
                    }
                }

                // Reindirizza alla pagina corretta in base al FileName
                if (comunicazioniDTO.FileName?.Contains("EBI") == true)
                {
                    return RedirectToAction("Index", "ComunicazionisEbi");
                }
                else if (comunicazioniDTO.FileName?.Contains("SSC") == true)
                {
                    return RedirectToAction("Index", "ComunicazionisSsc");
                }

                // Reindirizzamento predefinito
                return RedirectToAction("Index", "Home");
            }, null, this);
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                return int.Parse(_encryptionService.Decrypt(encryptedId));
            }
            catch
            {
                return -1;
            }
        }

        private void LogModelStateErrors()
        {
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
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
