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
            return View();
        }
        public IActionResult ComunicaionisSscController()
        {
            return View();
        }
        public IActionResult CreateController()
        {
            return View();
        }

        public IActionResult DestinatarisController()
        {
            return View();
        }

        [HttpGet("/Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Si è verificato un errore durante l'elaborazione della richiesta.");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
