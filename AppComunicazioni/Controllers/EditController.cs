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
        private readonly ISendMailService _sendMailService;
        private readonly IEncryptionService _encryptionService;

        public EditController(
            ComDbContext context,
            IMapper mapper,
            IRetryService retryService,
            IViewBagService viewBagService,
            ISendMailService sendMailService,
            IEncryptionService encryptionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            int decryptedId = DecryptId(id);
            if (decryptedId == -1)
                return BadRequest("ID non valido.");

            var comunicazioni = await _context.Comunicazionis.FindAsync(decryptedId);
            if (comunicazioni == null)
                return NotFound();

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
                return BadRequest("ID non valido.");

            int decryptedId = DecryptId(id);
            if (decryptedId == -1)
                return BadRequest("ID non valido.");

            comunicazioniDTO.Id = decryptedId;
            ModelState.Clear();
            TryValidateModel(comunicazioniDTO);

            if (!ModelState.IsValid)
            {
                LogModelStateErrors();
                ViewData["EncryptedId"] = id ?? string.Empty; // Assegna un valore predefinito per evitare null
                SetViewBagOptions();
                return View(comunicazioniDTO);
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(decryptedId);
                if (comunicazioniToUpdate == null)
                    return NotFound();

                _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

                if (comunicazioniDTO.DateF.HasValue)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    comunicazioniToUpdate.Email_inviata = false;
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                }
                else if (comunicazioniDTO.Ritornato)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    comunicazioniToUpdate.DateF = null;
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(comunicazioniDTO.FileName) && comunicazioniDTO.FileName.Contains("EBI"))
                {
                    return RedirectToAction("Index", "ComunicazionisEbi");
                }
                else if (!string.IsNullOrEmpty(comunicazioniDTO.FileName) && comunicazioniDTO.FileName.Contains("SSC"))
                {
                    return RedirectToAction("Index", "ComunicazionisSsc");
                }

                return RedirectToAction("Index", "Home");
            }, null, this);
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
