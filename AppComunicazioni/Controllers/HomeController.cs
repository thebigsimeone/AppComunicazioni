using AppComunicazioni.Data;
using AppComunicazioni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

namespace AppComunicazioni.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ComDbContext _context;

        public HomeController(ILogger<HomeController> logger, ComDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                Log.Information($"Tentativo di connessione al database: {conn.ConnectionString}");

                conn.Open();
                Log.Information("Connessione al database riuscita!");
                conn.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Errore di connessione al database: {ex.Message}");
            }

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
