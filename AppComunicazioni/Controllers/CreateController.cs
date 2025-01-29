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
    private readonly IFornitoreService _fornitoreService;

    public CreateController(
        ComDbContext context,
        IMapper mapper,
        IEmailService emailService,
        ILogger<CreateController> logger,
        IMonitoringService monitoringService,
        IExcelService excelService,
        IViewBagService viewBagService,
        IRetryService retryService,
        IFornitoreService fornitoreService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
        _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        _fornitoreService = fornitoreService ?? throw new ArgumentNullException(nameof(fornitoreService));
    }

    public IActionResult Index()
    {
        SetViewBagOptions();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note,Mandante,Fornitore,Report")] ComunicazioniDTO comunicazioniDTO, IFormFile? excelFile)
    {
        if (comunicazioniDTO == null)
        {
            _logger.LogError("Il DTO delle comunicazioni è nullo.");
            TempData["Message"] = "Errore: Il modulo è vuoto.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            SetViewBagOptions();
            TempData["Message"] = "Errore: I dati inseriti non sono validi.";
            return View(comunicazioniDTO);
        }

        await _retryService.ExecuteWithRetry(async () =>
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);

                        var fileNameParts = (comunicazioni.FileName ?? string.Empty).Split('_');
                        var mandante = fileNameParts.FirstOrDefault(part =>
                            part.Equals("SSC", StringComparison.OrdinalIgnoreCase) ||
                            part.Equals("EBI", StringComparison.OrdinalIgnoreCase));

                        if (mandante != null)
                        {
                            comunicazioni.Mandante = mandante;
                        }

                        comunicazioni.Fornitore = _fornitoreService.GetFornitoreByServizio(
                            Enum.Parse<ServizioType>(comunicazioni.Servizio, true),
                            comunicazioni.FileName).ToString();

                        comunicazioni.Notificato = false;
                        comunicazioni.Ritornato = false;
                        comunicazioni.NsProtocol = 0;
                        comunicazioni.Email_inviata = false;
                        comunicazioni.Report = "N";

                        _context.Add(comunicazioni);
                        await _context.SaveChangesAsync();

                        if (excelFile != null && excelFile.Length > 0)
                        {
                            var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioni.Id);
                            if (dettagli != null)
                            {
                                _context.ComunicazioniDettagli.AddRange(dettagli);
                                await _context.SaveChangesAsync();
                            }
                        }

                        await _monitoringService.CheckAndSendNotificationsAsync();
                        await transaction.CommitAsync();
                        TempData["Message"] = "Comunicazione creata con successo.";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Errore durante la creazione della comunicazione.");
                        TempData["Message"] = "Errore: Non è stato possibile completare l'operazione.";
                        throw;
                    }
                }
            });
            return RedirectToAction(nameof(Index));
        }, _logger, this);

        return RedirectToAction(nameof(Index));
    }

    private void SetViewBagOptions()
    {
        ViewBag.ServizioOptions = _viewBagService.GetServizioOptions() ?? new List<SelectListItem>();
    }
}
