using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AppComunicazioni.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Comunicazionis");
        }
        public IActionResult ComunicaionisController()
        {
            return View();
        }

        public IActionResult DestinatarisController()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
