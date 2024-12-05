using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public class ComunicazionisSscController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IMonitoringService _monitoringService;
        private readonly IFiltroComunicazioniService _filtroService;
        private readonly IExcelService _excelService;
        private readonly ISendMailService _sendMailService;
        private readonly IViewBagService _viewBagService;
        private readonly ILogger<ComunicazionisSscController> _logger;

        public ComunicazionisSscController(ComDbContext context, IMapper mapper, IEmailService emailService,
                                           IMonitoringService monitoringService, IFiltroComunicazioniService filtroComunicazioniService,
                                           IExcelService excelService, ISendMailService sendMailService,
                                           ILogger<ComunicazionisSscController> logger, IViewBagService viewBagService)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _filtroService = filtroComunicazioniService;
            _excelService = excelService;
            _monitoringService = monitoringService;
            _logger = logger;
            _sendMailService = sendMailService;
            _viewBagService = viewBagService;
        }

        // GET: ComunicazionisSsc
        public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate,
                                       string codCor, string servizio, DateTime? monthYear, bool? soloRigheNonRestituite = null,
                                       int pageNumber = 1, string sortField = "DateA", string sortOrder = "default")
        {
            SetViewBagOptions(codCor);

            // Recupera il valore di soloRigheNonRestituite dalla sessione se non fornito
            if (!soloRigheNonRestituite.HasValue)
            {
                soloRigheNonRestituite = bool.TryParse(HttpContext.Session.GetString("soloRigheNonRestituite"), out bool result) ? result : false;
            }

            // Salva i valori dei filtri nel ViewData per mantenerli nella vista
            ViewData["SearchTerm"] = searchTerm;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            ViewData["CodCor"] = codCor;
            ViewData["Servizio"] = servizio;
            ViewData["MonthYear"] = monthYear?.ToString("yyyy-MM");
            ViewData["SoloRigheNonRestituite"] = soloRigheNonRestituite;

            int pageSize = 10;
            var query = _context.Comunicazionis.AsQueryable();

            // Utilizza il servizio per applicare i filtri
            query = await _filtroService.FiltraComunicazioniAsync(query, "SSC", searchTerm, startDate, endDate, codCor, servizio, monthYear, sortField, sortOrder, soloRigheNonRestituite.Value);

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
                Servizio = servizio,
                MonthYear = monthYear,
                SortField = sortField,
                SortOrder = sortOrder,
                SearchTerm = searchTerm,
                SoloRigheNonRestituite = soloRigheNonRestituite.Value
            };

            return View(model);
        }

        private void SetViewBagOptions(string? codCor = null)
        {
            ViewBag.CodCorOptions = _viewBagService.GetCodCorOptions(codCor);
            ViewBag.ServizioOptions = _viewBagService.GetServizioOptions(codCor);
        }

        // GET: ComunicazionisSsc/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis
                                              .Include(c => c.Dettagli)  // Include i dettagli della comunicazione
                                              .FirstOrDefaultAsync(c => c.Id == id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            return View(comunicazioniDTO);
        }

        // GET: ComunicazionisSsc/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            SetViewBagOptions();
            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            return View(comunicazioniDTO);
        }

        // POST: ComunicazionisSsc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Servizio,Note,Ritornato")] ComunicazioniDTO comunicazioniDTO, IFormFile excelFile)
        {
            if (id != comunicazioniDTO.Id) return NotFound();

            // Rimuovi la convalida di excelFile
            ModelState.Remove("excelFile");

            if (!ModelState.IsValid)
            {
                SetViewBagOptions();
                return View(comunicazioniDTO);
            }

            var comunicazioniToUpdate = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioniToUpdate == null) return NotFound();

            // Mappa i valori aggiornati dal DTO al modello esistente
            _mapper.Map(comunicazioniDTO, comunicazioniToUpdate);

            // Imposta Ritornato a true se DateF è stato impostato
            if (comunicazioniDTO.DateF.HasValue)
            {
                comunicazioniToUpdate.Ritornato = true;
            }

            try
            {
                // Salva le modifiche nel database per i dettagli della comunicazione aggiornati dall'utente
                await _context.SaveChangesAsync();

                // Se il valore di "Ritornato" è true, interrompe il monitoraggio
                if (comunicazioniDTO.Ritornato == true)
                {
                    await _monitoringService.StopMonitoringForComunicazioneAsync(comunicazioniToUpdate.Id);
                }

                // Se la data di fine è stata impostata (smarco completato), invia email di smarco
                if (comunicazioniDTO.DateF.HasValue)
                {
                    await _sendMailService.HandlePostEditActionsAsync(comunicazioniToUpdate);
                }

                // Se un nuovo file Excel è stato caricato, processarlo e salvare i dettagli
                if (excelFile != null && excelFile.Length > 0)
                {
                    // Elimina i dettagli precedenti collegati alla comunicazione
                    var existingDetails = _context.ComunicazioniDettagli.Where(d => d.ComunicazioneId == comunicazioniToUpdate.Id);
                    _context.ComunicazioniDettagli.RemoveRange(existingDetails);
                    await _context.SaveChangesAsync();

                    // Processa il nuovo file Excel e salva i dettagli
                    var dettagli = await _excelService.ProcessExcelFileAsync(excelFile, comunicazioniToUpdate.Id);
                    if (dettagli != null)
                    {
                        _context.ComunicazioniDettagli.AddRange(dettagli);
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ComunicazioniExists(id)) return NotFound();
                throw;
            }
        }

        // GET: ComunicazionisSsc/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(m => m.Id == id);
            if (comunicazioni == null) return NotFound();

            return View(comunicazioni);
        }

        // POST: ComunicazionisSsc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comunicazioni = await _context.Comunicazionis
                .Include(c => c.Dettagli) // Includi i dettagli collegati
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comunicazioni != null)
            {
                // Elimina tutti i dettagli associati a questa comunicazione
                if (comunicazioni.Dettagli != null && comunicazioni.Dettagli.Any())
                {
                    _context.ComunicazioniDettagli.RemoveRange(comunicazioni.Dettagli);
                }

                // Elimina la comunicazione
                _context.Comunicazionis.Remove(comunicazioni);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ComunicazioniExists(int id) => _context.Comunicazionis.Any(e => e.Id == id);

        [HttpPost]
        public IActionResult ResetFiltri()
        {
            _filtroService.ResetFiltri();
            return RedirectToAction("Index");
        }
    }
}
