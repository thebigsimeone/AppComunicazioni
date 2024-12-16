using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Controllers
{
    public class EditController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRetryService _retryService;
        private readonly IViewBagService _viewBagService;
        private readonly IMonitoringService _monitoringService;
        private readonly ISendMailService _sendMailService;
        private readonly IExcelService _excelService;

        public EditController(ComDbContext context, IMapper mapper, IRetryService retryService,
                              IViewBagService viewBagService, IMonitoringService monitoringService,
                              ISendMailService sendMailService, IExcelService excelService)
        {
            _context = context;
            _mapper = mapper;
            _retryService = retryService;
            _viewBagService = viewBagService;
            _monitoringService = monitoringService;
            _sendMailService = sendMailService;
            _excelService = excelService;
        }

        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            if (!id.HasValue) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            SetViewBagOptions();
            return View(comunicazioniDTO);
        }

        // POST: Edit
        [HttpPost]
        public async Task<IActionResult> Index(int id, ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
        {
            if (id != comunicazioniDTO.Id) return NotFound();

            return await _retryService.ExecuteWithRetry(async () =>
            {
                if (!ModelState.IsValid)
                {
                    SetViewBagOptions();
                    return View(comunicazioniDTO);
                }

                var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(id);
                if (comunicazioniToUpdate == null) return NotFound();

                _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

                // Gestione logica DateF e Ritornato
                if (comunicazioniDTO.DateF.HasValue)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                    await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                }
                else if (comunicazioniDTO.Ritornato)
                {
                    comunicazioniToUpdate.Ritornato = true;
                    comunicazioniToUpdate.DateF = null;
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

                return RedirectToAction("Index", "Home");
            }, null, this);
        }

        private void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor) ?? new List<SelectListItem>();
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor) ?? new List<SelectListItem>();
        }
    }
}
