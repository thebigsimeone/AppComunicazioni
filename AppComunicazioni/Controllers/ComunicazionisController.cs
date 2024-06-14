using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AppComunicazioni.Utility;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace AppComunicazioni.Controllers
{
    public class ComunicazionisController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<ComunicazionisController> _logger;

        public ComunicazionisController(ComDbContext context, IMapper mapper, IEmailService emailService, ILogger<ComunicazionisController> logger)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Comunicazionis
        public async Task<IActionResult> Index(string searchTerm, DateTime? startDate, DateTime? endDate, int pageNumber = 1, string sortField = "DateA", string sortOrder = "default")
        {
            int pageSize = 10;
            IQueryable<Comunicazioni> query = _context.Comunicazionis;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(x => x.FileName.Contains(searchTerm));
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DateA >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DateF <= endDate.Value);
            }

            query = sortOrder switch
            {
                "asc" => sortField switch
                {
                    "FileName" => query.OrderBy(x => x.FileName),
                    "DateA" => query.OrderBy(x => x.DateA),
                    "DateF" => query.OrderBy(x => x.DateF),
                    "NProtocol" => query.OrderBy(x => x.NProtocol),
                    "NsProtocol" => query.OrderBy(x => x.NsProtocol),
                    _ => query.OrderBy(x => x.DateA),
                },
                "desc" => sortField switch
                {
                    "FileName" => query.OrderByDescending(x => x.FileName),
                    "DateA" => query.OrderByDescending(x => x.DateA),
                    "DateF" => query.OrderByDescending(x => x.DateF),
                    "NProtocol" => query.OrderByDescending(x => x.NProtocol),
                    "NsProtocol" => query.OrderByDescending(x => x.NsProtocol),
                    _ => query.OrderByDescending(x => x.DateA),
                },
                _ => query.OrderBy(x => x.DateA),
            };

            var totalItems = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var model = new ComunicazioniViewModel
            {
                Comunicazioni = items,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                StartDate = startDate,
                EndDate = endDate,
                SortField = sortField,
                SortOrder = sortOrder,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // GET: Comunicazionis/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            comunicazioniDTO.Note = ViewHelpers.FormatNoteForDisplay(comunicazioniDTO.Note);
            return View(comunicazioniDTO);
        }

        // GET: Comunicazionis/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Comunicazionis/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Note")] ComunicazioniDTO comunicazioniDTO)
        {
            if (ModelState.IsValid)
            {
                var comunicazioni = _mapper.Map<Comunicazioni>(comunicazioniDTO);
                _context.Add(comunicazioni);
                await _context.SaveChangesAsync();

                bool emailSuccess = true;
                var destinatari = await _context.Destinataris.ToListAsync();
                if (destinatari.Count == 0)
                {
                    _logger.LogWarning("Non ci sono destinatari");
                }
                else
                {
                    string subject = $"SMARCO ACCERTAMENTI DEL FILE: {comunicazioni.FileName}";
                    string formattedNote = FormatNote(comunicazioni.Note);
                    string message = $"<p>Il seguente file è stato aggiunto nell'area comunicazioni: {comunicazioni.FileName}<br>" +
                                     $"con il numero protocolli: {comunicazioni.NProtocol}<br><br>" +
                                     $"Questi sono i protocolli da controllare: {comunicazioni.NsProtocol}<br><br>" +
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

                TempData["Message"] = emailSuccess ? "Comunicazione creata e email inviate con successo." : "Comunicazione creata, ma l'invio delle email è fallito.";
                return RedirectToAction(nameof(Index));
            }
            return View(comunicazioniDTO);
        }


        private string FormatNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return note;

            var formattedNote = new StringBuilder();
            var lines = note.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    formattedNote.AppendLine($"{parts[0].Trim()} | {parts[1].Trim()}<br>");
                }
            }

            return formattedNote.ToString();
        }


        // GET: Comunicazionis/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FindAsync(id);
            if (comunicazioni == null) return NotFound();

            var comunicazioniDTO = _mapper.Map<ComunicazioniDTO>(comunicazioni);
            return View(comunicazioniDTO);
        }

        // POST: Comunicazionis/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,DateA,DateF,NProtocol,NsProtocol,Note")] ComunicazioniDTO comunicazioniDTO)
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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComunicazioniExists(id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(comunicazioniDTO);
        }

        // GET: Comunicazionis/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var comunicazioni = await _context.Comunicazionis.FirstOrDefaultAsync(m => m.Id == id);
            if (comunicazioni == null) return NotFound();

            return View(comunicazioni);
        }

        // POST: Comunicazionis/Delete/5
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
    }
}
