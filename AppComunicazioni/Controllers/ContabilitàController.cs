using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public ContabilitàController(IContabilitàService contabilitaService, ComDbContext context, IRetryService retryService, IViewBagService viewBagService, IFiltroComunicazioniService filtroService, ILogger<ContabilitàController> logger)
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
                // Configurazione ViewBag
                ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor);
                ViewBag.TipoAccertamentoOptions = new List<SelectListItem>
        {
            new SelectListItem { Value = "SSC", Text = "SSC", Selected = tipoAccertamento == "SSC" },
            new SelectListItem { Value = "EBI", Text = "EBI", Selected = tipoAccertamento == "EBI" }
        };

                ViewData["MeseAnno"] = meseAnno?.ToString("yyyy-MM");
                ViewData["CodCor"] = codCor;
                ViewData["TipoAccertamento"] = tipoAccertamento;

                var query = _context.Comunicazionis.AsQueryable();

                query = await _filtroService.FiltraComunicazioniAsync(
                    query,
                    tipoAccertamento ?? string.Empty,
                    string.Empty, // Nessun termine di ricerca
                    null,         // Nessuna data di inizio
                    null,         // Nessuna data di fine
                    codCor ?? string.Empty,
                    string.Empty, // Nessun filtro sul servizio
                    meseAnno,     // Filtra per mese/anno
                    "DateA",      // Campo predefinito per l'ordinamento
                    "asc",        // Ordinamento crescente
                    false         // Nessun filtro per righe non restituite
                );

                var model = await query
                    .GroupBy(x => x.Servizio)
                    .Select(g => new ContabilitaAccertamentiViewModel
                    {
                        Servizio = g.Key ?? "N/A",
                        TotaleAccertamenti = g.Sum(x => x.NProtocol ?? 0),
                        TotaleAccertamentiRitornati = g.Sum(x => x.DateF.HasValue ? x.NProtocol ?? 0 : 0),
                        TotaleAccertamentiMancanti = g.Sum(x => !x.DateF.HasValue ? x.NProtocol ?? 0 : 0)
                    })
                    .ToListAsync();

                return View(model);
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
