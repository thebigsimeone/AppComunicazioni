using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Utility;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public class ComunicazionisEbiController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IMonitoringService _monitoringService;
        private readonly IFiltroComunicazioniService _filtroService;
        private readonly ILogger<ComunicazionisEbiController> _logger;

        public ComunicazionisEbiController(ComDbContext context, IMapper mapper, IEmailService emailService, IMonitoringService monitoringService, IFiltroComunicazioniService filtroComunicazioniService, ILogger<ComunicazionisEbiController> logger)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _filtroService = filtroComunicazioniService;
            _monitoringService = monitoringService;
            _logger = logger;
        }

        // GET: ComunicazionisEbi
        public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate, string codCor, DateTime? monthYear, int pageNumber = 1, string sortField = "DateA", string sortOrder = "default")
        {
            ViewBag.CodCorOptions = Enum.GetValues(typeof(CodCorType))
                                        .Cast<CodCorType>()
                                        .Select(c => new SelectListItem
                                        {
                                            Value = c.ToString().Replace("COD_", ""),
                                            Text = c.GetDisplayName(),
                                            Selected = codCor != null && c.ToString().Replace("COD_", "") == codCor
                                        }).ToList();

            ViewData["SearchTerm"] = searchTerm;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            ViewData["MonthYear"] = monthYear?.ToString("yyyy-MM");
            ViewData["CodCor"] = codCor;

            int pageSize = 10;
            IQueryable<Comunicazioni> query = _context.Comunicazionis.AsQueryable();

            // Utilizza il servizio per applicare i filtri
            query = await _filtroService.FiltraComunicazioniAsync(query, "EBI", searchTerm, startDate, endDate, codCor, monthYear, sortField, sortOrder);

            var totalItems = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var model = new ComunicazioniViewModel
            {
                Comunicazioni = items,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                StartDate = startDate,
                EndDate = endDate,
                CodCor = codCor,
                MonthYear = monthYear,
                SortField = sortField,
                SortOrder = sortOrder,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // GET: ComunicazionisEbi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            comunicazioniDTO.Note = ViewHelpers.FormatNoteForDisplay(comunicazioniDTO.Note);
            return View(comunicazioniDTO);
        }

        // GET: ComunicazionisEbi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);

            // Passa le opzioni enum alla vista usando ViewBag
            ViewBag.ServizioOptions = Enum.GetValues(typeof(ServizioType))
                                          .Cast<ServizioType>()
                                          .Select(s => new SelectListItem
                                          {
                                              Value = s == ServizioType.S035 ? "035" : s.ToString(),
                                              Text = s == ServizioType.S035 ? "035" : s.ToString()
                                          }).ToList();

            return View(comunicazioniDTO);
        }

        // POST: ComunicazionisEbi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note")] ComunicazioniDTO comunicazioniDTO)
        {
            if (id != comunicazioniDTO.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(id);
                if (comunicazioniToUpdate == null) return NotFound();

                _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

                try
                {
                    await _context.SaveChangesAsync();

                    // Invia l'email solo se DateF è valorizzato
                    if (comunicazioniToUpdate.DateF != null && comunicazioniToUpdate.DateF != DateTimeOffset.MinValue)
                    {
                        bool emailSuccess = true;

                        // Seleziona solo i destinatari attivi
                        var destinatari = await _context.Destinataris
                            .Where(d => d.Attivo == "S")
                            .ToListAsync();

                        if (destinatari.Count == 0)
                        {
                            _logger.LogWarning("Non ci sono destinatari attivi.");
                        }
                        else
                        {
                            string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioniToUpdate.FileName}";
                            string formattedNote = _emailService.FormatNote(comunicazioniToUpdate.Note);
                            string message = $"<p>Il seguente file è stato smarcato: {comunicazioniToUpdate.FileName}<br>" +
                                             $"con il numero protocolli: {comunicazioniToUpdate.NProtocol}<br><br>" +
                                             $"Questi sono i protocolli da controllare: {comunicazioniToUpdate.NsProtocol}<br><br>" +
                                             $"{formattedNote}<br><br>" +
                                             $"Cordiali saluti,<br><br>" +
                                             $"Flavio Simeone</p>";

                            foreach (var destinatario in destinatari)
                            {
                                try
                                {
                                    await _emailService.SendEmailAsync(destinatario.Destinatario, subject, message);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Errore invio email a: {destinatario.Destinatario} | {ex.Message}");
                                    emailSuccess = false;
                                }
                            }
                        }

                        TempData["Message"] = emailSuccess ? "Comunicazione modificata e email inviate con successo." : "Comunicazione modificata, ma l'invio delle email è fallito.";
                    }
                    else
                    {
                        TempData["Message"] = "Comunicazione modificata senza inviare email perché DateF non è valorizzato.";
                    }

                    // Interrompi il monitoraggio per la comunicazione modificata
                    try
                    {
                        await _monitoringService.StopMonitoringForComunicazioneAsync(id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore durante l'interruzione del monitoraggio per la comunicazione modificata: {ex.Message}");
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComunicazioniExists(id)) return NotFound();
                    throw;
                }
            }
            return View(comunicazioniDTO);
        }

        // GET: ComunicazionisEbi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comunicazioni == null) return NotFound();

            return View(comunicazioni);
        }

        // POST: ComunicazionisEbi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni != null)
            {
                _context.Comunicazionis.Remove(comunicazioni);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ComunicazioniExists(int id)
        {
            return _context.Comunicazionis.Any(e => e.Id == id);
        }

        [HttpPost]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }

    }
}
