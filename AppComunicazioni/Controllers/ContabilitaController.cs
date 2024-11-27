using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;

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
        public async Task<IActionResult> Index(DateTime? meseAnno = null, string codCor = null)
        {
            // Configurazione del ViewBag per la select del CodCor
            ViewBag.CodCorOptions = Enum.GetValues(typeof(CodCorType))
                                        .Cast<CodCorType>()
                                        .Select(c => new SelectListItem
                                        {
                                            Value = c.ToString().Replace("COD_", ""),
                                            Text = c.GetDisplayName()
                                        }).ToList();

            var model = await _contabilitaService.GetTotaleAccertamentiAsync(meseAnno, codCor);
            return View(model);
        }
    }
}
