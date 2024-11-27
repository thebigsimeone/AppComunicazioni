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
                                            Value = c.ToString().Replace("COD_", ""),
                                            Text = c.GetDisplayName()
                                        }).ToList();

            // Configurazione del ViewBag per la select del TipoAccertamento (EBI o SSC)
            ViewBag.TipoAccertamentoOptions = new[]
            {
                new SelectListItem { Value = "SSC", Text = "SSC" },
                new SelectListItem { Value = "EBI", Text = "EBI" }
            };

            var model = await _contabilitaService.GetTotaleAccertamentiAsync(meseAnno, codCor, tipoAccertamento);
            return View(model);
        }
    }
}
