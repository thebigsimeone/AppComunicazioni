using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class CreateController : Controller
{
    private readonly ComDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateController> _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IExcelService _excelService;
    private readonly IViewBagService _viewBagService;
    private readonly IRetryService _retryService;

    public CreateController(ComDbContext context, IMapper mapper, IEmailService emailService,
                            ILogger<CreateController> logger, IMonitoringService monitoringService,
                            IExcelService excelService, IViewBagService viewBagService,
                            IRetryService retryService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
        _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
    }

    public IActionResult Index()
    {
        SetViewBagOptions();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note, ")] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        if (ModelState.IsValid)
        {
            if (comunicazioniDTO == null)
            {
                throw new ArgumentNullException(nameof(comunicazioniDTO));
            }

            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(comunicazioniDTO.FileName))
                        {
                            var fileNameParts = comunicazioniDTO.FileName.Split('_');
                            if (fileNameParts.Length > 3)
                            {
                                // Estrarre il servizio
                                var servicePart = fileNameParts[^2]; // Penultima parte della stringa

                                // Gestire caso particolare "035" -> "S035"
                                if (servicePart == "35")
                                {
                                    servicePart = "S035";
                                }

                                // Validare il servizio rispetto all'enum
                                if (Enum.TryParse<ServizioType>(servicePart, true, out var servizioParsed))
                                {
                                    comunicazioniDTO.Servizio = servizioParsed;
                                }
                                else
                                {
                                    _logger.LogWarning($"Il servizio '{servicePart}' non è valido per il file {comunicazioniDTO.FileName}.");
                                }

                                // Estrarre il numero di protocollo
                                var lastPart = fileNameParts.LastOrDefault();
                                if (int.TryParse(lastPart, out var protocolNumber))
                                {
                                    comunicazioniDTO.NProtocol = protocolNumber;
                                }
                            }
                        }

                        // Mappatura da DTO a Entity
                        var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);

                        // Impostare valori di default
                        comunicazioni.Notificato = false;
                        comunicazioni.Ritornato = false;
                        comunicazioni.NsProtocol = 0;

                        // Aggiungere la comunicazione al contesto
                        _context.Add(comunicazioni);
                        await _context.SaveChangesAsync();

                        // Elaborare il file Excel
                        if (excelFile != null && excelFile.Length > 0)
                        {
                            var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioni.Id);
                            if (dettagli != null)
                            {
                                _context.ComunicazioniDettagli.AddRange(dettagli);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Completa la transazione
                        await transaction.CommitAsync();
                        TempData["Message"] = "Comunicazione creata con successo.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }, _logger, this);
        }

        SetViewBagOptions();
        return View(comunicazioniDTO);
    }

    private void SetViewBagOptions()
    {
        ViewBag.ServizioOptions = _viewBagService.GetServizioOptions() ?? new List<SelectListItem>();
    }
}
