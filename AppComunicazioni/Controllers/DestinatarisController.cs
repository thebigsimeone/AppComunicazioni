using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Controllers
{
    public class DestinatarisController : Controller
    {
        private readonly ComDbContext _context;
        private readonly IMapper _mapper;

        public DestinatarisController(ComDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: Destinataris
        public async Task<IActionResult> Index()
        {
            return View(await _context.Destinataris.ToListAsync());
        }

        // GET: Destinataris/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris
                .FirstOrDefaultAsync(m => m.Id == id);
            if (destinatari == null)
            {
                return NotFound();
            }
            var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);
            return View(destinatariDTO);
        }

        // GET: Destinataris/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Destinataris/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Destinatario")] DestinatariDTO destinatariDTO)
        {
            if (ModelState.IsValid)
            {
                destinatariDTO.Monitor = Request.Form["Monitor"].Contains("true") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form["Attivo"].Contains("true") ? "S" : "N";

                var destinatari = _mapper.Map<Destinatari>(destinatariDTO);
                _context.Add(destinatari);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(destinatariDTO);
        }


        // GET: Destinataris/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris.FindAsync(id);
            if (destinatari == null)
            {
                return NotFound();
            }
            var destinatariDTO = _mapper.Map<DestinatariDTO>(destinatari);
            return View(destinatariDTO);
        }

        // POST: Destinataris/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Destinatario,Monitor,Attivo")] DestinatariDTO destinatariDTO)
        {
            if (id != destinatariDTO.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Se Monitor o Attivo non sono inviati, significa che la checkbox non è selezionata.
                destinatariDTO.Monitor = Request.Form.ContainsKey("Monitor") ? "S" : "N";
                destinatariDTO.Attivo = Request.Form.ContainsKey("Attivo") ? "S" : "N";

                try
                {
                    var destinatari = _mapper.Map<Destinatari>(destinatariDTO);
                    _context.Update(destinatari);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DestinatariExists(destinatariDTO.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(destinatariDTO);
        }

        // GET: Destinataris/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var destinatari = await _context.Destinataris
                .FirstOrDefaultAsync(m => m.Id == id);
            if (destinatari == null)
            {
                return NotFound();
            }

            return View(destinatari);
        }

        // POST: Destinataris/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var destinatari = await _context.Destinataris.FindAsync(id);
            if (destinatari != null)
            {
                _context.Destinataris.Remove(destinatari);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DestinatariExists(int id)
        {
            return _context.Destinataris.Any(e => e.Id == id);
        }
    }
}
