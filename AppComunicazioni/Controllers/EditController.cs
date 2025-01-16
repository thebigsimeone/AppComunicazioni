using AppComunicazioni.Data;
using AppComunicazioni.Interface;
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
        private readonly IExcelService _excelService;
        private readonly IStopMonitoringService _stopMonitoringService;

        public EditController(ComDbContext context, IMapper mapper,
                              IRetryService retryService, IViewBagService viewBagService,
                              ISendMailService sendMailService, IEncryptionService encryptionService,
                              ILogger<EditController> logger, IExcelService excelService, IStopMonitoringService stopMonitoringService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _sendMailService = sendMailService ?? throw new ArgumentNullException(nameof(sendMailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _stopMonitoringService = stopMonitoringService ?? throw new ArgumentNullException(nameof(stopMonitoringService));

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
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.Id == decryptedId);
            if (comunicazioni == null)
                return NotFound();

            _logger.LogInformation($"Email_inviata al caricamento: {comunicazioni.Email_inviata}");

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

            ViewData["EncryptedId"] = id ?? string.Empty;
            SetViewBagOptions();
            return View(comunicazioniDTO);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string Id, [Bind] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
        {
            if (string.IsNullOrEmpty(Id))
            {
                TempData["Message"] = "Errore: ID non valido.";
                return BadRequest("ID non valido.");
            }

            int decryptedId = DecryptId(Id);

            await _retryService.ExecuteWithRetry(async () =>
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var comunicazioniToUpdate = await _context.Comunicazionis.FirstOrDefaultAsync(c => c.Id == decryptedId);

                            comunicazioniToUpdate.DateF = comunicazioniDTO.DateF;
                            comunicazioniToUpdate.Note = comunicazioniDTO.Note;

                            if (comunicazioniToUpdate.Email_inviata.HasValue && !comunicazioniToUpdate.Email_inviata.Value)
                            {
                                await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                                comunicazioniToUpdate.Email_inviata = true;
                                comunicazioniToUpdate.Ritornato = true;
                            }

/*                          // Elaborazione del file Excel
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

                            await transaction.CommitAsync();
                            TempData["Message"] = "Modifica salvata con successo.";
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Errore durante la modifica della comunicazione.");
                            TempData["Message"] = "Errore durante la modifica.";
                            throw;
                        }
                    }
                });
                return RedirectToAction(nameof(Index));
            }, _logger, this);

            return RedirectToAction(nameof(Index));
        }

        private int DecryptId(string encryptedId)
        {
            try
            {
                return int.Parse(_encryptionService.Decrypt(encryptedId) ?? "-1");
            }
            catch
            {
                return -1;
            }
        }

        private void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor) ?? new List<SelectListItem>();
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor) ?? new List<SelectListItem>();
        }
    }
}
