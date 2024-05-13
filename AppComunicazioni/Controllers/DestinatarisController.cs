using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AppComunicazioni.Data;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;
using AutoMapper;

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Destinatario")] DestinatariDTO destinatariDTO)
        {
            var destinatari = _mapper.Map<Destinatari>(destinatariDTO);
            if (id != destinatari.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(destinatari);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DestinatariExists(destinatari.Id))
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
