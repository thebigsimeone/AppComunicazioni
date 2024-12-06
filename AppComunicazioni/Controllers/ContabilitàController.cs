using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Controllers
{
    public class ContabilitàController : Controller
    {
        private readonly IContabilitàService _contabilitaService;
        private readonly ComDbContext _context;
        private readonly IRetryService _retryService;
        private readonly IViewBagService _viewBagService;
        private readonly IFiltroComunicazioniService _filtroService;
        private readonly ILogger<ContabilitàController> _logger;

        public ContabilitàController(IContabilitàService contabilitaService, ComDbContext context, IRetryService retryService, IViewBagService viewBagService,IFiltroComunicazioniService filtroService, ILogger<ContabilitàController> logger)
        {
            _contabilitaService = contabilitaService ?? throw new ArgumentNullException(nameof(contabilitaService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _viewBagService = viewBagService ?? throw new ArgumentNullException(nameof(viewBagService));
            _filtroService = filtroService ?? throw new ArgumentNullException(nameof(filtroService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Contabilità
        public async Task<IActionResult> Index(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Utilizza il servizio ViewBagService per configurare le opzioni del ViewBag
                        ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor);
                        ViewBag.TipoAccertamentoOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "SSC", Text = "SSC", Selected = tipoAccertamento == "SSC" },
                        new SelectListItem { Value = "EBI", Text = "EBI", Selected = tipoAccertamento == "EBI" }
                    };

                        ViewData["MeseAnno"] = meseAnno?.ToString("yyyy-MM");
                        ViewData["CodCor"] = codCor;
                        ViewData["TipoAccertamento"] = tipoAccertamento;

                        if (_contabilitaService == null)
                        {
                            return NotFound("Servizio di contabilità non disponibile.");
                        }

                        // Recupera il totale degli accertamenti utilizzando il servizio di contabilità
                        var model = await _contabilitaService.GetTotaleAccertamentiAsync(meseAnno, codCor, tipoAccertamento);

                        await transaction.CommitAsync(); // Conferma la transazione se tutto è andato a buon fine
                        return View(model);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(); // Annulla la transazione in caso di errore
                        throw;
                    }
                }
            }, _logger, this);
        }

        [HttpPost]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
