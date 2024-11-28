using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
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

        public ComunicazionisEbiController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                           IMonitoringService monitoringService, IFiltroComunicazioniService filtroComunicazioniService,
                                           ILogger<ComunicazionisEbiController> logger)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _filtroService = filtroComunicazioniService;
            _monitoringService = monitoringService;
            _logger = logger;
        }

        // GET: ComunicazionisEbi
        public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate,
                                               string codCor, DateTime? monthYear, int pageNumber = 1,
                                               string sortField = "DateA", string sortOrder = "default")
        {
            SetViewBagOptions(codCor);

            int pageSize = 10;
            var query = _context.Comunicazionis.AsQueryable();

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
            return View(comunicazioniDTO);
        }

        // GET: ComunicazionisEbi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            SetViewBagOptions();
            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            return View(comunicazioniDTO);
        }

        // POST: ComunicazionisEbi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note")] ComunicazioniDTO comunicazioniDTO)
        {
            if (id != comunicazioniDTO.Id) return NotFound();

            if (!ModelState.IsValid) return View(comunicazioniDTO);

            var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioniToUpdate == null) return NotFound();

            _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

            try
            {
                await _context.SaveChangesAsync();
                await HandlePostEditActions(comunicazioniToUpdate);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ComunicazioniExists(id)) return NotFound();
                throw;
            }
        }

        private async Task HandlePostEditActions(Comunicazioni comunicazioniToUpdate)
        {
            if (comunicazioniToUpdate.DateF != null)
            {
                await SendNotificationEmails(comunicazioniToUpdate);
                await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
            }
        }

        private async Task SendNotificationEmails(Comunicazioni comunicazioni)
        {
            bool emailSuccess = true;
            var destinatari = await _context.Destinataris.Where(d => d.Attivo == "S").ToListAsync();

            if (destinatari.Count == 0)
            {
                _logger.LogWarning("Non ci sono destinatari attivi.");
            }
            else
            {
                string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioni.FileName}";
                string formattedNote = _emailService.FormatNote(comunicazioni.Note);
                string message = $"<p>Il seguente file è stato smarcato: {comunicazioni.FileName}<br>" +
                                 $"con il numero protocolli: {comunicazioni.NProtocol}<br><br>" +
                                 $"Questi sono i protocolli da controllare: {comunicazioni.NsProtocol}<br><br>" +
                                 $"{formattedNote}<br><br>" +
                                 $"Cordiali saluti,<br>Flavio Simeone</p>";

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

        // GET: ComunicazionisEbi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(m => m.Id == id);
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

        private bool ComunicazioniExists(int id) => _context.Comunicazionis.Any(e => e.Id == id);

        private void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = Enum.GetValues(typeof(CodCorType))
                                        .Cast<CodCorType>()
                                        .Select(c => new SelectListItem
                                        {
                                            Value = c.ToString(),
                                            Text = c.GetDisplayName(),
                                            Selected = codCor != null && codCor.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase)
                                        }).ToList();

            ViewBag.ServizioOptions = Enum.GetValues(typeof(ServizioType))
                                          .Cast<ServizioType>()
                                          .Select(s => new SelectListItem
                                          {
                                              Value = s.ToString(), // Utilizziamo il nome effettivo dell'enum per il valore
                                              Text = s.GetDisplayName() // Ottieni il nome visualizzato con DisplayAttribute
                                          }).ToList();
        }

        [HttpPost]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
