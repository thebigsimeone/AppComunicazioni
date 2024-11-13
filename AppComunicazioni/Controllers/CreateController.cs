using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppComunicazioni.Data;
using AutoMapper;
using System.Text;
using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Controllers
{
    public class CreateController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IMonitoringService _monitoringService;
        private readonly ILogger<CreateController> _logger;

        public CreateController(ComDbContext context, IMapper mapper, IEmailService emailService, ILogger<CreateController> logger, IMonitoringService monitoringService)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
            _monitoringService = monitoringService;
        }

        public IActionResult Index()
        {
            ViewBag.ServizioOptions = Enum.GetValues(typeof(ServizioType))
                                          .Cast<ServizioType>()
                                          .Select(s => new SelectListItem
                                          {
                                              Value = s.ToString(),
                                              Text = s.ToString()
                                          }).ToList();

            return View();
        }

        // POST: Comunicazionis/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note")] ComunicazioniDTO comunicazioniDTO)
        {
            if (ModelState.IsValid)
            {
                // Mappatura da DTO a Entity
                var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);
                _context.Add(comunicazioni);
                await _context.SaveChangesAsync();

                // Attiviamo il monitoraggio
                try
                {
                    await _monitoringService.CheckAndSendNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante l'avvio del monitoraggio dopo la creazione: {ex.Message}");
                }

                TempData["Message"] = "Comunicazione creata con successo. Il monitoraggio è stato avviato.";
                return RedirectToAction(nameof(Index));
            }

            return View(comunicazioniDTO);
        }
    }
}
