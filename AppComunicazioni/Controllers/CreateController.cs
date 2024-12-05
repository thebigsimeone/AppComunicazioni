using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;

public class CreateController : Controller
{
    private readonly ComDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateController> _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IExcelService _excelService;
    private readonly IViewBagService _viewBagService;

    public CreateController(ComDbContext context, IMapper mapper, IEmailService emailService,
                            ILogger<CreateController> logger, IMonitoringService monitoringService,
                            IExcelService excelService, IViewBagService viewBagService)
    {
        _context = context;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _monitoringService = monitoringService;
        _excelService = excelService;
        _viewBagService = viewBagService;
    }

    public IActionResult Index()
    {
        SetViewBagOptions();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note")] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Mappatura da DTO a Entity
                var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);

                // Imposta il valore di "Servizio" per la logica del DB (S035 se utente ha selezionato "035")
                comunicazioni.Servizio = comunicazioniDTO.Servizio == ServizioType.S035 ? "S035" : comunicazioniDTO.Servizio.ToString();

                // Imposta il valore di Notificato e Ritornato a false di default
                comunicazioni.Notificato = false;
                comunicazioni.Ritornato = false;

                _context.Add(comunicazioni);
                await _context.SaveChangesAsync();

                // Se l'utente ha caricato un file Excel, procedi con l'elaborazione
                if (excelFile != null && excelFile.Length > 0)
                {
                    var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioni.Id);
                    if (dettagli != null)
                    {
                        _context.ComunicazioniDettagli.AddRange(dettagli);
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation($"Comunicazione con ID {comunicazioni.Id} è stata creata e salvata correttamente.");

                try
                {
                    await Task.Delay(1000);
                    await _monitoringService.CheckAndSendNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante l'avvio del monitoraggio dopo la creazione: {ex.Message}");
                }

                TempData["Message"] = "Comunicazione creata con successo. Il monitoraggio è stato avviato.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante la creazione della comunicazione: {ex.Message}");
                ModelState.AddModelError("", "Si è verificato un errore durante la creazione della comunicazione. Riprova.");
            }
        }

        // Se il modello non è valido, ripopola le opzioni del ViewBag e ritorna alla vista Index
        SetViewBagOptions();
        return View("Index", comunicazioniDTO);
    }

    private void SetViewBagOptions()
    {
        ViewBag.ServizioOptions = _viewBagService.GetServizioOptions();
    }
}
