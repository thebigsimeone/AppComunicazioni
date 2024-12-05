using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public class ContabilitaController : Controller
    {
        private readonly IContabilitaService _contabilitaService;
        private readonly ComDbContext _context;
        private readonly IRetryService _retryService;
        private readonly ILogger<ContabilitaController> _logger;

        public ContabilitaController(IContabilitaService contabilitaService, ComDbContext context, IRetryService retryService, ILogger<ContabilitaController> logger)
        {
            _contabilitaService = contabilitaService ?? throw new ArgumentNullException(nameof(contabilitaService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Contabilita
        public async Task<IActionResult> Index(DateTime? meseAnno = null, string? codCor = null, string? tipoAccertamento = null)
        {
            return await _retryService.ExecuteWithRetry(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Configurazione del ViewBag per la select del CodCor
                        ViewBag.CodCorOptions = Enum.GetValues(typeof(CodCorType))
                                                    .Cast<CodCorType>()
                                                    .Select(c => new SelectListItem
                                                    {
                                                        Value = c.ToString(), // Usa il nome esatto dell'enum come valore per la logica di filtraggio
                                                        Text = c.GetDisplayName(), // Mostra il testo definito nel DisplayAttribute per una migliore UX
                                                        Selected = !string.IsNullOrEmpty(codCor) && codCor.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase)
                                                    }).ToList();

                        // Configurazione del ViewBag per la select del TipoAccertamento (EBI o SSC)
                        ViewBag.TipoAccertamentoOptions = new[]
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
    }
}
