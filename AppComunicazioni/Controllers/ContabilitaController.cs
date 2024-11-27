using AppComunicazioni.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppComunicazioni.Controllers
{
    public class ContabilitaController : Controller
    {
        private readonly IContabilitaService _contabilitaService;

        public ContabilitaController(IContabilitaService contabilitaService)
        {
            _contabilitaService = contabilitaService;
        }

        // GET: Contabilita
        public async Task<IActionResult> Index(DateTime? meseAnno = null, string codCor = null, string tipoAccertamento = null)
        {
            // Configurazione del ViewBag per la select del CodCor
            ViewBag.CodCorOptions = Enum.GetValues(typeof(CodCorType))
                                        .Cast<CodCorType>()
                                        .Select(c => new SelectListItem
                                        {
                                            Value = c.ToString(), // Usa il nome esatto dell'enum come valore per la logica di filtraggio
                                            Text = c.GetDisplayName(), // Mostra il testo definito nel DisplayAttribute per una migliore UX
                                            Selected = codCor != null && codCor.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase)
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

            var model = await _contabilitaService.GetTotaleAccertamentiAsync(meseAnno, codCor, tipoAccertamento);
            return View(model);
        }

    }
}
