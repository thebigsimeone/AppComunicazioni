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

    public CreateController(
        ComDbContext context,
        IMapper mapper,
        IEmailService emailService,
        ILogger<CreateController> logger,
        IMonitoringService monitoringService,
        IExcelService excelService,
        IViewBagService viewBagService,
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
    public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note")] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        if (comunicazioniDTO == null)
        {
            _logger.LogError("Il DTO delle comunicazioni è nullo.");
            throw new ArgumentNullException(nameof(comunicazioniDTO));
        }

        if (!ModelState.IsValid)
        {
            SetViewBagOptions();
            return View(comunicazioniDTO);
        }

        return await _retryService.ExecuteWithRetry(async () =>
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Estrai e valida i dati dal nome file
                    if (!string.IsNullOrEmpty(comunicazioniDTO.FileName))
                    {
                        var fileNameParts = comunicazioniDTO.FileName.Split('_');
                        var mandante = fileNameParts.FirstOrDefault(part => part.Equals("SSC", StringComparison.OrdinalIgnoreCase) || part.Equals("EBI", StringComparison.OrdinalIgnoreCase));
                        if (mandante != null)
                        {
                            comunicazioniDTO.Mandante = mandante;

                            // Trova l'indice del mandante e prendi la parte successiva come servizio
                            var mandanteIndex = Array.IndexOf(fileNameParts, mandante);
                            if (mandanteIndex >= 0 && mandanteIndex + 1 < fileNameParts.Length)
                            {
                                var servicePart = fileNameParts[mandanteIndex + 1];

                                if (servicePart == "035") servicePart = "S035";

                                if (Enum.TryParse<ServizioType>(servicePart, true, out var servizioParsed))
                                {
                                    comunicazioniDTO.Servizio = servizioParsed;
                                }
                                else
                                {
                                    _logger.LogWarning($"Servizio non valido: {servicePart} in file {comunicazioniDTO.FileName}");
                                }
                            }
                        }

                        // Estrai il numero di protocollo dalla parte finale
                        var lastPart = fileNameParts.LastOrDefault();
                        if (int.TryParse(lastPart, out var protocolNumber))
                        {
                            comunicazioniDTO.NProtocol = protocolNumber;
                        }
                    }

                    // Mappatura e valori di default
                    var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);
                    comunicazioni.Notificato = false;
                    comunicazioni.Ritornato = false;
                    comunicazioni.NsProtocol = 0;
                    comunicazioni.Email_inviata = false;

                    // Aggiungi comunicazione al contesto
                    _context.Add(comunicazioni);
                    await _context.SaveChangesAsync();

                    // Elaborazione del file Excel
                    if (excelFile != null && excelFile.Length > 0)
                    {
                        var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioni.Id);
                        if (dettagli != null)
                        {
                            _context.ComunicazioniDettagli.AddRange(dettagli);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Invoca il servizio di monitoraggio
                    await _monitoringService.CheckAndSendNotificationsAsync();

                    // Commit della transazione
                    await transaction.CommitAsync();
                    TempData["Message"] = "Comunicazione creata con successo.";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Errore durante la creazione della comunicazione.");
                    throw;
                }
            }
        }, _logger, this);
    }

    private void SetViewBagOptions()
    {
        ViewBag.ServizioOptions = _viewBagService.GetServizioOptions() ?? new List<SelectListItem>();
    }
}
